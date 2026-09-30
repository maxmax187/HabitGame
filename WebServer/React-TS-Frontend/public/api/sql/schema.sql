-- Run this once against the study database (e.g. via phpMyAdmin) before
-- using participant_admin.php / data_admin.php for the first time.
-- "condition" is a reserved word in MariaDB, hence condition_group.
--
-- Five conditions: a 2 x 2 between-subjects design of training length
-- (MODERATE = 1 day, EXTENSIVE = 3 days) x outcome manipulation (REMOVAL
-- vs DEVALUATION, handled in-game), balanced equally against each other.
-- SHORT is a separate single-session condition, excluded from
-- auto-balancing.
--
-- If you already have a live database from an earlier condition scheme,
-- do NOT run this file against it - use the latest migrate_*.sql instead
-- (currently migrate_to_2x2_conditions.sql).

CREATE TABLE IF NOT EXISTS participants (
  id INT AUTO_INCREMENT PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  condition_group ENUM('MODERATE_REMOVAL', 'MODERATE_DEVALUATION', 'EXTENSIVE_REMOVAL', 'EXTENSIVE_DEVALUATION', 'SHORT') NOT NULL,
  forced TINYINT(1) NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS game_data (
  id INT AUTO_INCREMENT PRIMARY KEY,
  participant_email VARCHAR(255) NOT NULL,
  day TINYINT NOT NULL,
  condition_group ENUM('MODERATE_REMOVAL', 'MODERATE_DEVALUATION', 'EXTENSIVE_REMOVAL', 'EXTENSIVE_DEVALUATION', 'SHORT') NOT NULL,
  data LONGTEXT NOT NULL,
  submitted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_participant_day (participant_email, day)
);
