-- Run this once against the study database (e.g. via phpMyAdmin) before
-- using participant_admin.php / data_admin.php for the first time.
-- "condition" is a reserved word in MariaDB, hence condition_group.
--
-- Five conditions: BETWEEN_L/BETWEEN_R/WITHIN_L/WITHIN_R all run the full
-- 3 days and are balanced equally against each other (BETWEEN = tested
-- only at day 3, WITHIN = tested at day 1 and day 3, both handled
-- in-game, crossed with L/R bias). SHORT is a separate single-session
-- condition, excluded from auto-balancing.
--
-- If you already have a live database from an earlier condition scheme
-- (L/R, or ML/MR/EL/ER), do NOT run this file against it - use
-- migrate_to_schedule_conditions.sql instead.

CREATE TABLE IF NOT EXISTS participants (
  id INT AUTO_INCREMENT PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  condition_group ENUM('BETWEEN_L', 'BETWEEN_R', 'WITHIN_L', 'WITHIN_R', 'SHORT') NOT NULL,
  forced TINYINT(1) NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS game_data (
  id INT AUTO_INCREMENT PRIMARY KEY,
  participant_email VARCHAR(255) NOT NULL,
  day TINYINT NOT NULL,
  condition_group ENUM('BETWEEN_L', 'BETWEEN_R', 'WITHIN_L', 'WITHIN_R', 'SHORT') NOT NULL,
  data LONGTEXT NOT NULL,
  submitted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_participant_day (participant_email, day)
);
