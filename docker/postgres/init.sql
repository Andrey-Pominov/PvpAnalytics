CREATE USER auth WITH PASSWORD 'auth123';
ALTER ROLE auth WITH LOGIN;
CREATE DATABASE authdb OWNER auth;
GRANT ALL PRIVILEGES ON DATABASE authdb TO auth;

-- Create pvp role if it doesn't exist (may already exist from POSTGRES_USER)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'pvp') THEN
        CREATE ROLE pvp WITH PASSWORD 'pvp_password' LOGIN;
    ELSE
        -- Ensure password and login are set correctly
        ALTER ROLE pvp WITH PASSWORD 'pvp_password' LOGIN;
    END IF;
END
$$;

-- Create paymentdb database owned by pvp
CREATE DATABASE IF NOT EXISTS paymentdb OWNER pvp;
GRANT ALL PRIVILEGES ON DATABASE paymentdb TO pvp;

