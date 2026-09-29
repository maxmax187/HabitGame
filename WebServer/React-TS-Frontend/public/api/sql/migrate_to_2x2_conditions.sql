-- Run this ONCE against the live database to move from the schedule-based
-- conditions (BETWEEN_L/BETWEEN_R/WITHIN_L/WITHIN_R + SHORT) to the 2 x 2
-- design: MODERATE_REMOVAL/MODERATE_DEVALUATION (1 day),
-- EXTENSIVE_REMOVAL/EXTENSIVE_DEVALUATION (3 days), plus SHORT.
--
-- The old conditions have no equivalent in the new design, and the study
-- owner confirmed only test data exists, so this CLEARS BOTH TABLES
-- (including SHORT rows) instead of remapping them. Download a backup
-- from data_admin.php first if anything in there is still needed.
--
-- Do NOT run this against a fresh database set up with the current
-- schema.sql - there's nothing to migrate.

TRUNCATE TABLE game_data;
TRUNCATE TABLE participants;

ALTER TABLE participants MODIFY COLUMN condition_group
  ENUM('MODERATE_REMOVAL', 'MODERATE_DEVALUATION', 'EXTENSIVE_REMOVAL', 'EXTENSIVE_DEVALUATION', 'SHORT') NOT NULL;
ALTER TABLE game_data MODIFY COLUMN condition_group
  ENUM('MODERATE_REMOVAL', 'MODERATE_DEVALUATION', 'EXTENSIVE_REMOVAL', 'EXTENSIVE_DEVALUATION', 'SHORT') NOT NULL;
