-- Run this using an administrator account in MySQL Workbench.
-- Replace kali123! with your own password, then set the same password in your connection string.
CREATE DATABASE IF NOT EXISTS daily_routine_db
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'daily_routine_user'@'localhost'
    IDENTIFIED BY 'kali123!';

-- If the user already exists, set its password with:
-- ALTER USER 'daily_routine_user'@'localhost' IDENTIFIED BY 'kali123!';

GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, INDEX, ALTER, REFERENCES
    ON daily_routine_db.* TO 'daily_routine_user'@'localhost';

FLUSH PRIVILEGES;
