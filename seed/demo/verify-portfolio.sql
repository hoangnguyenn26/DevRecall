-- Read-only initial-scenario smoke, run before personally changing this account.
-- psql -v ON_ERROR_STOP=1 ... -f seed/demo/verify-portfolio.sql
BEGIN READ ONLY;
DO $$
DECLARE
    demo uuid := 'd0900000-0000-0000-0000-000000000001';
BEGIN
    ASSERT (SELECT count(*) FROM users WHERE id = demo AND email = 'portfolio@devrecall.local') = 1, 'Missing portfolio user';
    ASSERT (SELECT count(*) FROM learning_profiles WHERE user_id = demo) = 1, 'Profile count';
    ASSERT (SELECT count(*) FROM learning_content_progresses WHERE user_id = demo AND status = 2) = 6, 'Completed count';
    ASSERT (SELECT count(*) FROM learning_content_progresses WHERE user_id = demo AND status = 1) = 1, 'InProgress count';
    ASSERT (SELECT count(*) FROM learning_content_completion_evidence WHERE user_id = demo) = 6, 'Evidence count';
    ASSERT NOT EXISTS (
        SELECT 1 FROM learning_content_progresses p
        LEFT JOIN learning_content_completion_evidence e ON e.user_id = p.user_id AND e.learning_content_id = p.learning_content_id
        WHERE p.user_id = demo AND ((p.status = 2 AND (e.id IS NULL OR e.completed_at_utc <> p.completed_at_utc))
            OR (p.status = 1 AND e.id IS NOT NULL))), 'Progress/evidence mismatch';
    ASSERT NOT EXISTS (
        SELECT 1 FROM learning_contents c JOIN learning_content_progresses p ON p.learning_content_id = c.id
        WHERE p.user_id = demo AND c.content_type = 2), 'External resource progress';
    ASSERT NOT EXISTS (
        SELECT 1 FROM learning_contents c JOIN learning_content_completion_evidence e ON e.learning_content_id = c.id
        WHERE e.user_id = demo AND c.content_type = 2), 'External resource evidence';
    ASSERT (SELECT count(*) FROM review_items WHERE user_id = demo) = 10, 'Card count';
    ASSERT (SELECT count(*) FROM review_histories h JOIN review_items r ON r.id = h.review_item_id WHERE r.user_id = demo) = 20, 'Rating count';
    ASSERT NOT EXISTS (SELECT 1 FROM review_items r WHERE r.user_id = demo AND r.review_count <>
        (SELECT count(*) FROM review_histories h WHERE h.review_item_id = r.id)), 'Rating count mismatch';
    ASSERT (SELECT count(*) FROM knowledge_nodes WHERE user_id = demo) = 5, 'Knowledge count';
    ASSERT (SELECT count(*) FROM study_plans WHERE user_id = demo) = 1, 'Plan count';
    ASSERT (SELECT count(*) FROM study_sessions WHERE user_id = demo) = 1, 'Session count';
    ASSERT (SELECT actual_duration_minutes FROM study_sessions WHERE user_id = demo) = 34, 'Elapsed time, not estimated minutes';
    ASSERT NOT EXISTS (SELECT 1 FROM study_plan_items i JOIN study_plans p ON p.id = i.study_plan_id
        WHERE i.id::text LIKE 'd0900000-%' AND p.user_id <> demo), 'Plan ownership';
    ASSERT NOT EXISTS (SELECT 1 FROM study_session_items i JOIN study_sessions s ON s.id = i.study_session_id
        WHERE i.id::text LIKE 'd0900000-%' AND s.user_id <> demo), 'Session ownership';
    ASSERT (SELECT count(DISTINCT completed_at_utc::date) FROM learning_content_completion_evidence WHERE user_id = demo) = 6, 'History chronology';
    ASSERT NOT EXISTS (SELECT 1 FROM learning_content_completion_evidence e JOIN users u ON u.id = e.user_id
        WHERE e.user_id = demo AND e.completed_at_utc - u.created_at_utc NOT IN
            (interval '2 days', interval '6 days', interval '10 days', interval '14 days', interval '17 days', interval '20 days')),
        'Completion timestamps must derive from one seed anchor';
    ASSERT NOT EXISTS (SELECT 1 FROM study_session_items i JOIN study_sessions s ON s.id = i.study_session_id
        JOIN learning_contents c ON c.id = i.resource_id
        WHERE s.user_id = demo AND c.content_type = 1 AND NOT EXISTS
            (SELECT 1 FROM learning_content_completion_evidence e WHERE e.user_id = demo AND e.learning_content_id = c.id)),
        'Lesson session completion requires canonical evidence';
END $$;
ROLLBACK;
