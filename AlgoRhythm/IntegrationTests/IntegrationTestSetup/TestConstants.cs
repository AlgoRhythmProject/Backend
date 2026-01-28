namespace IntegrationTests.IntegrationTestSetup;

public static class TestConstants
{
    // JWT
    public const string TestJwtKey = "super_secret_test_key_12345678910";
    public const string TestJwtIssuer = "TestIssuer";
    public const string TestJwtAudience = "TestAudience";

    // Test user
    public const string TestUserEmail = "test.submission@test.com";
    public const string TestUserPassword = "SecurePwd123!";
    public const string TestUserSecurityStamp = "SomeTestSecurityStamp";

    // Sendgrid
    public const string TestSendGridApiKey = "TestApiKey";
    public const string TestSendGridFromName = "TestFromName";
    public const string TestSendGridFromEmail = "TestFromEmail";

    // URLs
    public const string TestFrontendUrl = "http://frontend:8888";
    public const string TestCodeExecutorUrl = "http://executor:11";

    // Azure
    public const string TestAzureConnectionString = "Azure";
}