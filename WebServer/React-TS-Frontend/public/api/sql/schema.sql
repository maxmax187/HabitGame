-- Run this once against the study database (e.g. via phpMyAdmin) before
-- using participant_admin.php / data_admin.php for the first time.
-- "condition" is a reserved word in MariaDB, hence condition_group.
--
-- Four conditions, equally balanced: ML/MR = moderate (1 day),
-- EL/ER = extensive (3 days), each crossed with L/R bias.
--
-- If you already have a live database from before this 4-condition
-- structure existed (when it was just 'L'/'R'), do NOT run this file
-- against it - use migrate_to_4_conditions.sql instead, which upgrades
-- the existing tables in place without losing data.

CREATE TABLE IF NOT EXISTS participants (
  id INT AUTO_INCREMENT PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  condition_group ENUM('ML', 'MR', 'EL', 'ER') NOT NULL,
  forced TINYINT(1) NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS game_data (
  id INT AUTO_INCREMENT PRIMARY KEY,
  participant_email VARCHAR(255) NOT NULL,
  day TINYINT NOT NULL,
  condition_group ENUM('ML', 'MR', 'EL', 'ER') NOT NULL,
  data LONGTEXT NOT NULL,
  submitted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_participant_day (participant_email, day)
);
