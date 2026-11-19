using Chronify;
using Chronify.Services;
using Chronify.Teams;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskStatus = Chronify.TaskStatus;

public static class NeonChecks
{
    public static async Task<int> Run(
        UserAccount first,
        UserAccount second,
        Func<AppDbContext> factory
    )
    {
        int checks = 0;
        var cloudUsers = new List<Guid>();
        void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }

            checks++;
        }
        async Task Denied(Func<Task> action, string message)
        {
            bool rejected = false;
            try
            {
                await action();
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected, message);
        }
        UserAccount ownerIdentity = null!, workerIdentity = null!;
        TeamService admin = null!, worker = null!;
        var ownerLogin = new AuthenticationService(new TestTokens(), factory);
        var workerLogin = new AuthenticationService(new TestTokens(), factory);
        const string password = "Neon-Checks-Password!";
        string adminToken = "", workerToken = "";
        try
        {
            ownerIdentity = await ownerLogin.RegisterAsync(
                first.DisplayName,
                Guid.NewGuid() + "@example.test",
                password,
                false
            );
            admin = new TeamService(ownerIdentity, factory);
            cloudUsers.Add(admin.Connection!.RemoteUserId!.Value);
            workerIdentity = await workerLogin.RegisterAsync(
                second.DisplayName,
                Guid.NewGuid() + "@example.test",
                password,
                false
            );
            worker = new TeamService(workerIdentity, factory);
            cloudUsers.Add(worker.Connection!.RemoteUserId!.Value);
            adminToken = new ConnectionStore(ownerIdentity.Id, factory).Secret("ChronifyNeon");
            workerToken = new ConnectionStore(workerIdentity.Id, factory).Secret("ChronifyNeon");
            using (var localDb = factory())
            {
                Check(localDb.Users.Any(u => u.Id == ownerIdentity.Id)
                    && localDb.Users.Any(u => u.Id == workerIdentity.Id)
                && ownerIdentity.Id == admin.Connection.RemoteUserId && workerIdentity.Id == worker.Connection.RemoteUserId,
                "Single registration must create matching accounts in Neon and SQLite and establish group sessions");
            }

            await ownerLogin.LogoutAsync();
            await Denied(async () =>
            {
                await NeonGateway.Request<object>("groups/list", token: adminToken);
            },
                "Application logout must revoke the Neon session too");
            ownerIdentity = await ownerLogin.LoginAsync(ownerIdentity.Email, password, false);
            adminToken = new ConnectionStore(ownerIdentity.Id, factory).Secret("ChronifyNeon");
            Check(
                new Chronify.ViewModels.GroupsViewModel(ownerIdentity, factory).IsConnected,
                "Ordinary application login must authorize groups automatically"
            );
            string deviceDirectory = Path.Combine(
                Path.GetTempPath(),
                "ChronifyNeonDevice",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(deviceDirectory);
            Func<AppDbContext> anotherDevice = () => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={Path.Combine(
                deviceDirectory,
                "local.db"
            )}")
                .Options);
            using (var deviceDb = anotherDevice())
            {
                DatabaseInitializer.Initialize(deviceDb);
            }

            var deviceLogin = new AuthenticationService(new TestTokens(), anotherDevice);
            var deviceUser = await deviceLogin.LoginAsync(ownerIdentity.Email, password, false);
            using (var deviceDb = anotherDevice())
            {
                Check(deviceDb.Users.Single().Id == ownerIdentity.Id
                && new TeamService(deviceUser, anotherDevice).Connection?.RemoteUserId == ownerIdentity.Id,
                "Normal login on a fresh computer must cache the shared identity and grant group access");
            }

            await deviceLogin.LogoutAsync();
            var lead = await NeonGateway.Request<CloudSession>(
                "register",
                new CloudCredentials(
                Guid.NewGuid() + "@example.test",
                password,
                "Lead"
            )
            );
            cloudUsers.Add(lead.UserId);
            var stranger = await NeonGateway.Request<CloudSession>(
                "register",
                new CloudCredentials(
                Guid.NewGuid() + "@example.test",
                password,
                "Stranger"
            )
            );
            cloudUsers.Add(stranger.UserId);
            await Denied(
                async () =>
                {
                    await NeonGateway.Request<CloudSession>(
                                           "login",
                                           new CloudCredentials(
                                           ownerIdentity.Email,
                                           "Wrong-Password!"
                                       )
                                       );
                },
                "Wrong password must be rejected"
            );
            var login = await NeonGateway.Request<CloudSession>(
                "login",
                new CloudCredentials(
                ownerIdentity.Email,
                password
            )
            );
            Check(login.UserId == admin.Connection.RemoteUserId, "Cloud login must restore the same identity");
            await admin.CreateGroup("Neon verification");
            await admin.Synchronize();
            var group = admin.Groups().Single();
            Check(group.Role == TeamRole.Administrator, "Creator must be administrator");
            var ownerView = new Chronify.ViewModels.GroupsViewModel(ownerIdentity, factory);
            Check(ownerView.Members.Count == 1, "Owner initially sees only the group creator");
            await worker.Join(group.InviteCode);
            await worker.Join(group.InviteCode);
            await worker.Synchronize();
            Check(worker.Groups().Single().Role == TeamRole.NoRole && worker.Members(group.Id).Count == 0
                && worker.Tasks(group.Id)
                .Count == 0,
                "New member must have no role, no member list and no tasks");
            var pending = new Chronify.ViewModels.GroupsViewModel(workerIdentity, factory);
            await ownerView.RefreshAutomatically();
            Check(ownerView.Members.Any(member => member.Id == workerIdentity.Id && member.Role == TeamRole.NoRole),
                "Automatic refresh must reveal a newly joined member to the owner without a manual refresh");
            ownerView.SelectedMember = ownerView.Members.Single(member => member.Id == workerIdentity.Id);
            ownerView.TaskTitle = "Draft during refresh";
            await ownerView.RefreshAutomatically();
            Check(ownerView.SelectedMember?.Id == workerIdentity.Id && ownerView.TaskTitle == "Draft during refresh",
                "Automatic refresh must preserve the selected group member and assignment draft");
            Check(
                !pending.CanViewGroup && !pending.CanAssign && pending.GroupDetails.Contains("Ожидает"),
                "UI must show only pending approval for NoRole"
            );
            await Denied(
                () => worker.ChangeRole(
                group.Id,
                worker.Connection.RemoteUserId!.Value,
                TeamRole.Administrator
            ),
                "NoRole must not escalate privileges"
            );
            var assignment = new CreateAssignmentRequest(
                worker.Connection.RemoteUserId!.Value,
                "Offline Neon task",
                "Conditions stored locally",
                DateTime.UtcNow.AddDays(1),
                30,
                3,
                0
            );
            await Denied(() => admin.Assign(group.Id, assignment), "Task must not be assigned to a NoRole member");
            await admin.ChangeRole(group.Id, worker.Connection.RemoteUserId.Value, TeamRole.Executor);
            await pending.RefreshAutomatically();
            Check(pending.CanViewGroup && pending.SelectedGroup?.Role == TeamRole.Executor && pending.Members.Count == 2,
                "The joining user's role and member list must update automatically after administrator approval");
            await NeonGateway.Request<object>("groups/join", new JoinGroupRequest(group.InviteCode), lead.Token);
            await admin.ChangeRole(group.Id, lead.UserId, TeamRole.TeamLead);
            await admin.Assign(group.Id, assignment);
            await admin.Assign(
                group.Id,
                assignment with { AssigneeId = lead.UserId, Title = "Lead assignment", Status = 1 }
            );
            await NeonGateway.Request<object>(
                "groups/tasks/create",
                new { groupId = group.Id, request = assignment with { Title = "Lead to worker", Status = 1 } },
                lead.Token
            );
            await Denied(() => worker.Assign(group.Id, assignment), "Executor must not assign tasks");
            await Denied(
                async () =>
                {
                    await NeonGateway.Request<GroupDetails>(
                                           "groups/details",
                                           new { groupId = group.Id },
                                           stranger.Token
                                       );
                },
                "Non-member must not read group"
            );
            await Denied(
                async () =>
                {
                    await NeonGateway.Request<object>(
                                           "groups/tasks/create",
                                           new { groupId = group.Id, request = assignment with { AssigneeId = lead.UserId } },
                                           lead.Token
                                       );
                },
                "Lead must not assign to lead"
            );
            await worker.Join(group.InviteCode);
            await worker.Synchronize();
            Check(worker.Groups().Single().Role == TeamRole.Executor, "Rejoining must not change an assigned role");
            Check(
                worker.Tasks(group.Id).Count == 2
                && worker.Tasks(group.Id).All(t => t.AssigneeId == worker.Connection.RemoteUserId),
                "Executor must see only own tasks"
            );
            var leadView = await NeonGateway.Request<GroupDetails>(
                "groups/details",
                new { groupId = group.Id },
                lead.Token
            );
            Check(leadView.Tasks.Count == 3, "Lead must see all assignments");
            await pending.RefreshAutomatically();
            Check(pending.Members.Any(member => member.Id == lead.UserId) && pending.Tasks.Count == 2,
                "Existing participants must receive new members and assigned tasks through automatic refresh");
            await admin.Synchronize();
            Check(admin.Members(group.Id).Count == 3, "Repeated joins must not duplicate members");
            var repository = new SqliteTaskRepository(workerIdentity.Id, factory);
            var local = repository.GetAll().Single(t => t.Title == assignment.Title);
            Check(local.Description == assignment.Description && local.Category?.Name == group.Name
                && local.AssignedByName == first.DisplayName,
                "Real Neon assignment must be copied into user's SQLite");
            await Denied(
                () => worker.Start(group.Id, local.GroupAssignmentId!.Value),
                "Only sender may start assignment"
            );
            await admin.Start(group.Id, local.GroupAssignmentId!.Value);
            await worker.Synchronize();
            local = repository.GetById(local.Id)!;
            local.CompleteTask();
            repository.Update(local);
            using (var db = factory())
            {
                Check(db.CachedAssignments.Single(t => t.UserId == workerIdentity.Id
                    && t.Id == local.GroupAssignmentId)
                    .PendingCompletion,
                "Offline completion must be queued in SQLite without network call");
            }

            await worker.Synchronize();
            await worker.Synchronize();
            await admin.Synchronize();
            Check(
                admin.Tasks(group.Id).Single(t => t.Id == local.GroupAssignmentId).Status == 2,
                "Queued completion must reach Neon"
            );
            Check(repository.GetAll().Count(t => t.GroupAssignmentId == local.GroupAssignmentId) == 1
                && repository.GetById(local.Id)!.Status == TaskStatus.Completed,
                "Repeated synchronization must not duplicate or reopen task");
            var remote = await NeonGateway.Request<GroupDetails>(
                "groups/details",
                new { groupId = group.Id },
                workerToken
            );
            // PostgreSQL зберігає мікросекунди, DateTime — крок 100 нс.
            var savedCompletion = remote.Tasks.Single(t => t.Id == local.GroupAssignmentId).CompletedAt!.Value.ToUniversalTime();
            Check(
                Math.Abs((savedCompletion - local.CompletedAt!.Value.ToUniversalTime()).Ticks) <= 10,
                "Offline completion time must survive Neon roundtrip within PostgreSQL microsecond precision"
            );
            await using var restricted = new NpgsqlConnection(NeonConfiguration.RuntimeConnection);
            await restricted.OpenAsync();
            bool blocked = false;
            try
            {
                await using var query = new NpgsqlCommand("SELECT * FROM chronify.users", restricted); await query.ExecuteScalarAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "42501")
            {
                blocked = true;
            }
            Check(blocked, "Client SQL role must not read tables directly");
            Check(
                !TeamRules.CanAssign(TeamRole.Administrator, TeamRole.NoRole),
                "NoRole must not receive assignments in UI rules either"
            );
            Console.WriteLine($"Neon end-to-end: PASS ({checks} checks, real database; local SQLite included).");
            return checks;
        }
        finally
        {
            // Видаляємо лише створені тут тестові акаунти та їхні групи. Робочі записи не зачіпаємо.
            await using var connection = new NpgsqlConnection(NeonConfiguration.ConnectionString(NeonConfiguration.Read("DATABASE_URL_UNPOOLED")!));
            await connection.OpenAsync();
            await using var cleanup = new NpgsqlCommand(
                "DELETE FROM chronify.users WHERE id=ANY(@ids)",
                connection
            );
            cleanup.Parameters.AddWithValue("ids", cloudUsers.ToArray());
            await cleanup.ExecuteNonQueryAsync();
            Console.WriteLine("Neon test accounts and groups: removed.");
        }
    }

    private sealed class TestTokens : ISessionTokenStore
    {
        private string? _value;
        public string? Read()
        {
            return _value;
        }

        public void Write(string token)
        {
            _value = token;
        }

        public void Clear()
        {
            _value = null;
        }
    }
}
