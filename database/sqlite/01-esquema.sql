-- Esquema do Claquete para SQLite (mesmas tabelas e colunas do script SQL Server,
-- com chaves estrangeiras e índices para as consultas mais usadas).

CREATE TABLE Atores (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    PrimeiroNome VARCHAR(20),
    UltimoNome   VARCHAR(20),
    Genero       VARCHAR(1) CHECK (Genero IN ('M', 'F'))
);

CREATE TABLE Filmes (
    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome    VARCHAR(50),
    Ano     INT,
    Duracao INT
);

CREATE TABLE Generos (
    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
    Genero VARCHAR(20)
);

CREATE TABLE ElencoFilme (
    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
    IdAtor  INT NOT NULL REFERENCES Atores (Id) ON DELETE CASCADE,
    IdFilme INT REFERENCES Filmes (Id) ON DELETE CASCADE,
    Papel   VARCHAR(30)
);

CREATE TABLE FilmesGenero (
    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
    IdGenero INT REFERENCES Generos (Id) ON DELETE CASCADE,
    IdFilme  INT REFERENCES Filmes (Id) ON DELETE CASCADE
);

CREATE INDEX IX_Filmes_Ano ON Filmes (Ano);
CREATE INDEX IX_ElencoFilme_IdFilme ON ElencoFilme (IdFilme);
CREATE INDEX IX_ElencoFilme_IdAtor ON ElencoFilme (IdAtor);
CREATE INDEX IX_FilmesGenero_IdFilme ON FilmesGenero (IdFilme);
CREATE INDEX IX_FilmesGenero_IdGenero ON FilmesGenero (IdGenero);
