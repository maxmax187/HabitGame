-- Run this ONCE against your existing live database to move from the old
-- dosage-based 4-condition structure (ML/MR = moderate/1 day, EL/ER =
-- extensive/3 days) to the new schedule-based 5-condition structure
-- (BETWEEN_L/BETWEEN_R/WITHIN_L/WITHIN_R, all 3 days, plus a separate
-- single-session SHORT condition).
--
-- Unlike the previous migration, this is NOT a rename: the old dosage
-- axis (1 day vs 3 days) has no equivalent under the new schedule axis
-- (both BETWEEN and WITHIN now run all 3 days), so old ML/MR/EL/ER rows
-- cannot be relabeled into the new scheme without misrepresenting how
-- many days that participant actually played. Confirmed with the study
-- owner that no real (non-test) participants/data exist yet under the
-- old scheme, so this clears both tables rather than remapping them.
--
-- Do NOT run this against a fresh database that was set up with
-- schema.sql already using the new 5-value enum - there's nothing to
-- migrate.

TRUNCATE TABLE game_data;
TRUNCATE TABLE participants;

ALTER TABLE participants MODIFY COLUMN condition_group
  ENUM('BETWEEN_L', 'BETWEEN_R', 'WITHIN_L', 'WITHIN_R', 'SHORT') NOT NULL;
ALTER TABLE game_data MODIFY COLUMN condition_group
  ENUM('BETWEEN_L', 'BETWEEN_R', 'WITHIN_L', 'WITHIN_R', 'SHORT') NOT NULL;
