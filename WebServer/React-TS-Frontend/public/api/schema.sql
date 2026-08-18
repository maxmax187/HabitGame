-- Run this once against the study database (e.g. via phpMyAdmin) before
-- using admin.php. "condition" is a reserved word in MariaDB, hence
-- condition_group.

CREATE TABLE IF NOT EXISTS participants (
  id INT AUTO_INCREMENT PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  condition_group ENUM('L', 'R') NOT NULL,
  forced TINYINT(1) NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS game_data (
  id INT AUTO_INCREMENT PRIMARY KEY,
  participant_email VARCHAR(255) NOT NULL,
  day TINYINT NOT NULL,
  condition_group ENUM('L', 'R') NOT NULL,
  data LONGTEXT NOT NULL,
  submitted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_participant_day (participant_email, day)
);
