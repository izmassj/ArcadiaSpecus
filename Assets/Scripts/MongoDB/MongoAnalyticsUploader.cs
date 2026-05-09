using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using UnityEngine;

public class MongoAnalyticsUploader : MonoBehaviour
{
    public static MongoAnalyticsUploader Instance { get; private set; }

    [Header("MongoDB")]
    [SerializeField] private string _connectionString = "mongodb+srv://a24ismsaajdi_db_user:5EtcmevqNTEZBCez@arcadiaanalytics.rrmb4nx.mongodb.net/?appName=ArcadiaAnalytics";
    [SerializeField] private string _databaseName = "ArcadiaAnalytics";
    [SerializeField] private string _sessionsCollectionName = "sessions";

    private MongoClient _client;
    private IMongoDatabase _database;
    private IMongoCollection<BsonDocument> _sessionsCollection;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        _client = new MongoClient(_connectionString);
        _database = _client.GetDatabase(_databaseName);
        _sessionsCollection = _database.GetCollection<BsonDocument>(_sessionsCollectionName);
    }

    public async Task UploadSessionAsync(GameSessionAnalyticsData data)
    {
        if (data == null)
            return;

        try
        {
            string json = JsonUtility.ToJson(data, false);
            BsonDocument document = BsonDocument.Parse(json);
            document["uploadedAtUtc"] = DateTime.UtcNow;

            await _sessionsCollection.InsertOneAsync(document);
            Debug.Log("Analytics uploaded to MongoDB: " + data.sessionId);
        }
        catch (Exception exception)
        {
            Debug.LogError("MongoDB analytics upload failed: " + exception.Message);
        }
    }

    [ContextMenu("Upload Current Session")]
    private async void UploadCurrentSessionFromInspector()
    {
        if (GameAnalyticsManager.Instance == null)
            return;

        await UploadSessionAsync(GameAnalyticsManager.Instance.CurrentData);
    }
}