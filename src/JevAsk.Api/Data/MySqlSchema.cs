namespace JevAsk.Api.Data;

public static class MySqlSchema
{
    public const string CreateQuestions = """
        CREATE TABLE `Questions` (
            `Id` bigint NOT NULL AUTO_INCREMENT,
            `Question` varchar(500) NOT NULL,
            `Ticker` varchar(16) NOT NULL,
            `Probability` double NOT NULL,
            `PayloadJson` longtext NOT NULL,
            `CreatedAtUtc` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`)
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
        """;

    public const string CreateSeries = """
        CREATE TABLE `Series` (
            `Ticker` varchar(16) NOT NULL,
            `BarsJson` longtext NOT NULL,
            `Origin` varchar(64) NOT NULL,
            `StoredAtUtc` datetime(6) NOT NULL,
            PRIMARY KEY (`Ticker`)
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
        """;

    public const string CreateQuestionsIndex =
        "CREATE INDEX `IX_Questions_CreatedAtUtc` ON `Questions` (`CreatedAtUtc`);";

    public const string CreateFunCards = """
        CREATE TABLE `FunCards` (
            `Id` bigint NOT NULL AUTO_INCREMENT,
            `Question` varchar(500) NOT NULL,
            `Likelihood` int NOT NULL,
            `Reasoning` longtext NOT NULL,
            `Category` varchar(64) NOT NULL,
            `Tag` varchar(32) NOT NULL,
            PRIMARY KEY (`Id`)
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
        """;

    public const string CreateFunCardsIndex =
        "CREATE INDEX `IX_FunCards_Category` ON `FunCards` (`Category`);";

    public const string CreateFunAsks = """
        CREATE TABLE `FunAsks` (
            `Id` bigint NOT NULL AUTO_INCREMENT,
            `Question` varchar(500) NOT NULL,
            `Likelihood` double NOT NULL,
            `Reasoning` longtext NOT NULL,
            `Category` varchar(64) NOT NULL,
            `Tag` varchar(32) NOT NULL,
            `Source` varchar(16) NOT NULL,
            `CreatedAtUtc` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`)
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
        """;

    public const string CreateFunAsksIndex =
        "CREATE INDEX `IX_FunAsks_CreatedAtUtc` ON `FunAsks` (`CreatedAtUtc`);";
}
