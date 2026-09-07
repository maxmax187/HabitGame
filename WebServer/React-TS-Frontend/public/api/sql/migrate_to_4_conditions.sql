-- Run this ONCE against your existing live database to upgrade it from
-- the old 2-condition structure ('L'/'R', always 3 days) to the new
-- 4-condition structure (ML/MR = moderate/1 day, EL/ER = extensive/3
-- days). Safe to run via phpMyAdmin or DBadmin.php's SQL runner.
--
-- Every existing 'L'/'R' participant and game_data row was playing the
-- (only) 3-day version, so they become 'EL'/'ER' - no data is lost or
-- reassigned, this is purely a rename to fit the new vocabulary.
--
-- Do NOT run this against a fresh database that was set up with
-- schema.sql already using ML/MR/EL/ER - there's nothing to migrate.

-- Step 1: widen the enum so both old and new values are valid at once
ALTER TABLE participants MODIFY COLUMN condition_group ENUM('L', 'R', 'ML', 'MR', 'EL', 'ER') NOT NULL;
ALTER TABLE game_data MODIFY COLUMN condition_group ENUM('L', 'R', 'ML', 'MR', 'EL', 'ER') NOT NULL;

-- Step 2: rename existing values
UPDATE participants SET condition_group = 'EL' WHERE condition_group = 'L';
UPDATE participants SET condition_group = 'ER' WHERE condition_group = 'R';
UPDATE game_data SET condition_group = 'EL' WHERE condition_group = 'L';
UPDATE game_data SET condition_group = 'ER' WHERE condition_group = 'R';

-- Step 3: narrow the enum back down to just the 4 final values
ALTER TABLE participants MODIFY COLUMN condition_group ENUM('ML', 'MR', 'EL', 'ER') NOT NULL;
ALTER TABLE game_data MODIFY COLUMN condition_group ENUM('ML', 'MR', 'EL', 'ER') NOT NULL;
