CREATE TABLE PLAYER (
    ID       SERIAL PRIMARY KEY,
    USERNAME TEXT   UNIQUE      NOT NULL,
    PASSWORD TEXT               NOT NULL
);
