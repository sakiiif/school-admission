namespace SchoolAdmission.Tests.Support;

// One shared container for every test class. Collections run sequentially,
// so tests never fight over the process-wide environment variables.
[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;