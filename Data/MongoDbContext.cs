using MongoDB.Driver;
using SprintTracker.Api.Models;

namespace SprintTracker.Api.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly string _collectionName;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDB:ConnectionString"];
        var databaseName = configuration["MongoDB:DatabaseName"];
    _collectionName = configuration["MongoDB:CollectionName"] ?? "Sprintallica";

     var client = new MongoClient(connectionString);
   _database = client.GetDatabase(databaseName);

        // Create indexes for better performance
        CreateIndexes();
    }

    private void CreateIndexes()
    {
   // Sprint indexes
        var sprintCollection = Sprints;
        var sprintIndexBuilder = Builders<Sprint>.IndexKeys;
sprintCollection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Sprint>(sprintIndexBuilder.Ascending(s => s.ProjectId)),
    new CreateIndexModel<Sprint>(sprintIndexBuilder.Ascending(s => s.Status)),
        new CreateIndexModel<Sprint>(sprintIndexBuilder.Descending(s => s.StartDate))
  });

        // Task indexes
        var taskCollection = Tasks;
        var taskIndexBuilder = Builders<SprintTask>.IndexKeys;
        taskCollection.Indexes.CreateMany(new[]
   {
            new CreateIndexModel<SprintTask>(taskIndexBuilder.Ascending(t => t.SprintId)),
            new CreateIndexModel<SprintTask>(taskIndexBuilder.Ascending(t => t.AssigneeId)),
     new CreateIndexModel<SprintTask>(taskIndexBuilder.Ascending(t => t.Status)),
            new CreateIndexModel<SprintTask>(taskIndexBuilder.Ascending(t => t.Priority))
      });

        // User indexes
    var userCollection = Users;
        var userIndexBuilder = Builders<User>.IndexKeys;
        userCollection.Indexes.CreateOne(
  new CreateIndexModel<User>(userIndexBuilder.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }));

     // Sprint Submission indexes
        var submissionCollection = SprintSubmissions;
        var submissionIndexBuilder = Builders<SprintSubmission>.IndexKeys;
 submissionCollection.Indexes.CreateMany(new[]
        {
 new CreateIndexModel<SprintSubmission>(submissionIndexBuilder.Ascending(s => s.SprintId)),
        new CreateIndexModel<SprintSubmission>(submissionIndexBuilder.Ascending(s => s.UserId)),
            new CreateIndexModel<SprintSubmission>(submissionIndexBuilder.Combine(
     submissionIndexBuilder.Ascending(s => s.SprintId),
 submissionIndexBuilder.Ascending(s => s.UserId)
            ), new CreateIndexOptions { Unique = true })
        });
    }

    public IMongoCollection<Sprint> Sprints => _database.GetCollection<Sprint>($"{_collectionName}_Sprints");
    public IMongoCollection<SprintTask> Tasks => _database.GetCollection<SprintTask>($"{_collectionName}_Tasks");
 public IMongoCollection<Project> Projects => _database.GetCollection<Project>($"{_collectionName}_Projects");
    public IMongoCollection<User> Users => _database.GetCollection<User>($"{_collectionName}_Users");
    public IMongoCollection<SprintMetrics> Metrics => _database.GetCollection<SprintMetrics>($"{_collectionName}_Metrics");
    public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>($"{_collectionName}_Comments");
  public IMongoCollection<ActivityLog> ActivityLogs => _database.GetCollection<ActivityLog>($"{_collectionName}_ActivityLogs");
    public IMongoCollection<SprintSubmission> SprintSubmissions => _database.GetCollection<SprintSubmission>($"{_collectionName}_SprintSubmissions");
}
