-- Grant additional necessary permissions for migrations
GRANT CREATE TABLE TO library;
GRANT CREATE SEQUENCE TO library;
GRANT CREATE VIEW TO library;
GRANT CREATE PROCEDURE TO library;
GRANT CREATE TRIGGER TO library;
GRANT UNLIMITED TABLESPACE TO library;

-- Often required for DB tools to work correctly
GRANT RESOURCE TO library;
GRANT CONNECT TO library;