-- Таблиці закриті для клієнта. Усі дії виконуються через request з перевіркою сесії та ролі.
CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;
CREATE SCHEMA IF NOT EXISTS chronify;
CREATE TABLE IF NOT EXISTS chronify.users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), email text NOT NULL UNIQUE,
    name text NOT NULL, password_hash text NOT NULL
);
CREATE TABLE IF NOT EXISTS chronify.sessions (
    token_hash text PRIMARY KEY, user_id uuid NOT NULL REFERENCES chronify.users(id) ON DELETE CASCADE,
    expires_at timestamptz NOT NULL
);
CREATE TABLE IF NOT EXISTS chronify.groups (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name text NOT NULL,
    invite_code text NOT NULL UNIQUE, owner_id uuid NOT NULL REFERENCES chronify.users(id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS chronify.members (
    group_id uuid NOT NULL REFERENCES chronify.groups(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES chronify.users(id) ON DELETE CASCADE,
    role integer NOT NULL DEFAULT -1 CHECK (role IN (-1,0,1,2)), PRIMARY KEY(group_id,user_id)
);
CREATE UNIQUE INDEX IF NOT EXISTS one_group_admin ON chronify.members(group_id) WHERE role=2;
CREATE TABLE IF NOT EXISTS chronify.tasks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), group_id uuid NOT NULL REFERENCES chronify.groups(id) ON DELETE CASCADE,
    assignee_id uuid NOT NULL, assigned_by_id uuid NOT NULL,
    title text NOT NULL, description text NOT NULL, due_date timestamptz NULL,
    estimated_minutes integer NOT NULL CHECK (estimated_minutes BETWEEN 0 AND 100000),
    priority integer NOT NULL CHECK (priority BETWEEN 0 AND 4), status integer NOT NULL CHECK(status BETWEEN 0 AND 2),
    completed_at timestamptz NULL,
    FOREIGN KEY(group_id,assignee_id) REFERENCES chronify.members(group_id,user_id) ON DELETE CASCADE,
    FOREIGN KEY(group_id,assigned_by_id) REFERENCES chronify.members(group_id,user_id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS members_user ON chronify.members(user_id);
CREATE INDEX IF NOT EXISTS tasks_group ON chronify.tasks(group_id);

CREATE OR REPLACE FUNCTION chronify.request(p_token text, p_action text, p_payload jsonb DEFAULT '{}')
RETURNS jsonb LANGUAGE plpgsql SECURITY DEFINER SET search_path = chronify, pg_temp AS $$
DECLARE
    v_user uuid; v_group uuid; v_target uuid; v_task uuid; v_role integer; v_target_role integer;
    v_email text; v_password text; v_name text; v_hash text; v_token text; v_code text;
    v_request jsonb; v_group_info jsonb; v_members jsonb; v_tasks jsonb; v_date timestamptz;
BEGIN
    v_request := COALESCE(p_payload->'request',p_payload);
    IF p_action IN ('register','login','loginOrRegister') THEN
        v_email := upper(trim(p_payload->>'email'));
        v_password := p_payload->>'password';
        IF v_email IS NULL OR v_password IS NULL OR octet_length(v_password) NOT BETWEEN 8 AND 72
            OR length(v_email)>200 OR v_email !~ '^[^[:space:]@]+@[^[:space:]@]+\.[^[:space:]@]+$' THEN
            RAISE EXCEPTION 'Проверьте почту и пароль (8–72 байта UTF-8).';
        END IF;
        -- Старий локальний акаунт переноситься лише після перевірки його пароля у застосунку.
        IF p_action='register' OR (p_action='loginOrRegister' AND NOT EXISTS(SELECT 1 FROM users WHERE email=v_email)) THEN
            v_name := trim(p_payload->>'name');
            IF v_name IS NULL OR length(v_name) NOT BETWEEN 1 AND 80 THEN RAISE EXCEPTION 'Имя: от 1 до 80 символов.'; END IF;
            INSERT INTO users(email,name,password_hash) VALUES(v_email,v_name,public.crypt(v_password,public.gen_salt('bf',11))) RETURNING id INTO v_user;
        ELSE
            SELECT id,password_hash INTO v_user,v_hash FROM users WHERE email=v_email;
            IF v_user IS NULL OR public.crypt(v_password,v_hash) IS DISTINCT FROM v_hash THEN RAISE EXCEPTION 'Неверная почта или пароль.'; END IF;
        END IF;
        v_token := encode(public.gen_random_bytes(32),'hex');
        DELETE FROM sessions WHERE expires_at<now();
        INSERT INTO sessions VALUES(encode(public.digest(v_token,'sha256'),'hex'),v_user,now()+interval '30 days');
        RETURN jsonb_build_object('userId',v_user,'token',v_token,'name',(SELECT name FROM users WHERE id=v_user));
    END IF;

    SELECT user_id INTO v_user FROM sessions WHERE token_hash=encode(public.digest(p_token,'sha256'),'hex') AND expires_at>now();
    IF v_user IS NULL THEN RAISE EXCEPTION 'Сессия истекла. Войдите в Chronify снова.'; END IF;
    IF p_action='logout' THEN
        DELETE FROM sessions WHERE token_hash=encode(public.digest(p_token,'sha256'),'hex');
        RETURN '{}'::jsonb;
    END IF;
    IF p_action='groups/list' THEN
        RETURN COALESCE((SELECT jsonb_agg(jsonb_build_object('id',g.id,'name',g.name,'role',m.role,
            'inviteCode',CASE WHEN m.role=2 THEN g.invite_code ELSE '' END))
            FROM groups g JOIN members m ON m.group_id=g.id WHERE m.user_id=v_user),'[]'::jsonb);
    END IF;
    IF p_action='groups/create' THEN
        v_name := trim(p_payload->>'name');
        IF v_name IS NULL OR length(v_name) NOT BETWEEN 1 AND 80 THEN RAISE EXCEPTION 'Название группы: от 1 до 80 символов.'; END IF;
        INSERT INTO groups(name,invite_code,owner_id) VALUES(v_name,upper(encode(public.gen_random_bytes(8),'hex')),v_user) RETURNING id INTO v_group;
        INSERT INTO members VALUES(v_group,v_user,2);
        RETURN '{}'::jsonb;
    END IF;
    IF p_action='groups/join' THEN
        SELECT id INTO v_group FROM groups WHERE invite_code=upper(trim(p_payload->>'code'));
        IF v_group IS NULL THEN RAISE EXCEPTION 'Группа с таким кодом не найдена.'; END IF;
        INSERT INTO members VALUES(v_group,v_user,-1) ON CONFLICT DO NOTHING;
        RETURN '{}'::jsonb;
    END IF;

    v_group := (p_payload->>'groupId')::uuid;
    SELECT role INTO v_role FROM members WHERE group_id=v_group AND user_id=v_user;
    IF v_role IS NULL THEN RAISE EXCEPTION 'Нет доступа к группе.'; END IF;
    IF p_action='groups/details' THEN
        SELECT jsonb_build_object('id',g.id,'name',g.name,'role',v_role,
            'inviteCode',CASE WHEN v_role=2 THEN g.invite_code ELSE '' END) INTO v_group_info FROM groups g WHERE id=v_group;
        IF v_role=-1 THEN RETURN jsonb_build_object('group',v_group_info,'members','[]'::jsonb,'tasks','[]'::jsonb); END IF;
        SELECT COALESCE(jsonb_agg(jsonb_build_object('id',u.id,'name',u.name,'role',m.role)),'[]'::jsonb)
            INTO v_members FROM members m JOIN users u ON u.id=m.user_id WHERE m.group_id=v_group;
        SELECT COALESCE(jsonb_agg(jsonb_build_object('id',t.id,'groupId',t.group_id,'title',t.title,'description',t.description,
            'dueDate',t.due_date,'estimatedMinutes',t.estimated_minutes,'priority',t.priority,'status',t.status,
            'assigneeId',t.assignee_id,'assigneeName',a.name,'assignedById',t.assigned_by_id,'assignedByName',s.name,
            'completedAt',t.completed_at)),'[]'::jsonb)
            INTO v_tasks FROM tasks t JOIN users a ON a.id=t.assignee_id JOIN users s ON s.id=t.assigned_by_id
            WHERE t.group_id=v_group AND (v_role IN (1,2) OR t.assignee_id=v_user);
        RETURN jsonb_build_object('group',v_group_info,'members',v_members,'tasks',v_tasks);
    END IF;
    IF v_role=-1 THEN RAISE EXCEPTION 'Роль ещё не назначена администратором.'; END IF;
    IF p_action='groups/roles/set' THEN
        v_target := (p_payload->>'memberId')::uuid;
        SELECT role INTO v_target_role FROM members WHERE group_id=v_group AND user_id=v_target;
        IF v_role<>2 OR v_target_role IS NULL OR v_target_role=2 OR (v_request->>'role')::integer NOT IN (0,1)
            OR v_request->>'role' IS NULL THEN RAISE EXCEPTION 'Только администратор назначает исполнителя или тимлида.'; END IF;
        UPDATE members SET role=(v_request->>'role')::integer WHERE group_id=v_group AND user_id=v_target;
        RETURN '{}'::jsonb;
    END IF;
    IF p_action='groups/tasks/create' THEN
        v_target := (v_request->>'assigneeId')::uuid;
        SELECT role INTO v_target_role FROM members WHERE group_id=v_group AND user_id=v_target;
        IF v_target_role IS NULL OR v_target=v_user OR NOT ((v_role=2 AND v_target_role IN (0,1)) OR (v_role=1 AND v_target_role=0))
            THEN RAISE EXCEPTION 'Недостаточно прав для назначения этому участнику.'; END IF;
        v_name := trim(v_request->>'title');
        v_date := (v_request->>'dueDate')::timestamptz;
        IF v_name IS NULL OR length(v_name) NOT BETWEEN 1 AND 200 OR length(COALESCE(v_request->>'description',''))>50000
            OR v_date<now() OR (v_request->>'status')::integer NOT IN (0,1) THEN RAISE EXCEPTION 'Проверьте название, срок и статус.'; END IF;
        INSERT INTO tasks(group_id,assignee_id,assigned_by_id,title,description,due_date,estimated_minutes,priority,status)
            VALUES(v_group,v_target,v_user,v_name,COALESCE(v_request->>'description',''),v_date,
                (v_request->>'estimatedMinutes')::integer,(v_request->>'priority')::integer,(v_request->>'status')::integer);
        RETURN '{}'::jsonb;
    END IF;
    v_task := (p_payload->>'taskId')::uuid;
    IF p_action='groups/tasks/start' THEN
        UPDATE tasks SET status=1 WHERE id=v_task AND group_id=v_group AND assigned_by_id=v_user AND status=0;
        IF NOT FOUND THEN RAISE EXCEPTION 'Начать новую задачу может только постановщик.'; END IF;
        RETURN '{}'::jsonb;
    END IF;
    IF p_action='groups/tasks/complete' THEN
        v_date := COALESCE((v_request->>'completedAtUtc')::timestamptz,now());
        IF v_date>now()+interval '5 minutes' THEN RAISE EXCEPTION 'Время завершения не может быть в будущем.'; END IF;
        UPDATE tasks SET status=2,completed_at=COALESCE(completed_at,v_date)
            WHERE id=v_task AND group_id=v_group AND assignee_id=v_user AND status IN (1,2);
        IF NOT FOUND THEN RAISE EXCEPTION 'Завершить начатую задачу может только её исполнитель.'; END IF;
        RETURN '{}'::jsonb;
    END IF;
    RAISE EXCEPTION 'Неизвестное действие.';
END;
$$;
REVOKE ALL ON SCHEMA chronify FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA chronify FROM PUBLIC;
REVOKE ALL ON FUNCTION chronify.request(text,text,jsonb) FROM PUBLIC;
