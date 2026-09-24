-- Melhorias opcionais para o banco Filmes no SQL Server.
-- Rode depois de 01-criar-banco.sql. O script original já cria as chaves estrangeiras,
-- mas sem ação na exclusão: apagar um filme com elenco falha. Aqui elas são trocadas
-- por versões com ON DELETE CASCADE, e entram uma regra de gênero e índices.
-- Pode ser executado mais de uma vez.

USE Filmes;
GO

-- Remove as chaves originais (nomes gerados pelo SQL Server no script) e as desta versão, se existirem.
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
FROM sys.foreign_keys
WHERE OBJECT_NAME(parent_object_id) IN (N'ElencoFilme', N'FilmesGenero');
EXEC sp_executesql @sql;
GO

ALTER TABLE ElencoFilme ADD CONSTRAINT FK_ElencoFilme_Atores
    FOREIGN KEY (IdAtor) REFERENCES Atores (Id) ON DELETE CASCADE;
ALTER TABLE ElencoFilme ADD CONSTRAINT FK_ElencoFilme_Filmes
    FOREIGN KEY (IdFilme) REFERENCES Filmes (Id) ON DELETE CASCADE;
ALTER TABLE FilmesGenero ADD CONSTRAINT FK_FilmesGenero_Generos
    FOREIGN KEY (IdGenero) REFERENCES Generos (Id) ON DELETE CASCADE;
ALTER TABLE FilmesGenero ADD CONSTRAINT FK_FilmesGenero_Filmes
    FOREIGN KEY (IdFilme) REFERENCES Filmes (Id) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'CK_Atores_Genero', N'C') IS NULL
    ALTER TABLE Atores ADD CONSTRAINT CK_Atores_Genero CHECK (Genero IN ('M', 'F'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Filmes_Ano')
    CREATE INDEX IX_Filmes_Ano ON Filmes (Ano);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ElencoFilme_IdFilme')
    CREATE INDEX IX_ElencoFilme_IdFilme ON ElencoFilme (IdFilme);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ElencoFilme_IdAtor')
    CREATE INDEX IX_ElencoFilme_IdAtor ON ElencoFilme (IdAtor);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FilmesGenero_IdFilme')
    CREATE INDEX IX_FilmesGenero_IdFilme ON FilmesGenero (IdFilme);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FilmesGenero_IdGenero')
    CREATE INDEX IX_FilmesGenero_IdGenero ON FilmesGenero (IdGenero);
GO
