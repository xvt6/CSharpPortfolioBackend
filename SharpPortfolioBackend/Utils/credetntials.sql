CREATE USER library IDENTIFIED BY password; --set a real password here, make sure to update .env file

GRANT CREATE SESSION TO library;

GRANT CREATE TABLE TO library;
GRANT CREATE VIEW TO library;
GRANT CREATE PROCEDURE TO library;
GRANT CREATE SEQUENCE TO library;

GRANT UNLIMITED TABLESPACE TO library;