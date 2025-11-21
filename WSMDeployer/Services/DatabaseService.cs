using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using WSMDeployer.Models;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Service for managing SQLite database operations
    /// Handles all CRUD operations for targets, deployments, jobs, configurations, and health checks
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseService(string? databasePath = null)
        {
            // Default database location: %ProgramData%\VertexDeployer\deployer.db
            _databasePath = databasePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "VertexDeployer", "deployer.db");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={_databasePath}";
            InitializeDatabase();
        }

        /// <summary>
        /// Initialize database schema - creates tables if they don't exist
        /// </summary>
        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTables = @"
                -- Targets table
                CREATE TABLE IF NOT EXISTS Targets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Hostname TEXT NOT NULL,
                    IPAddress TEXT,
                    Type INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    CredentialProfileId INTEGER,
                    VertexVersion TEXT,
                    LastSeen TEXT,
                    CreatedDate TEXT NOT NULL,
                    ModifiedDate TEXT NOT NULL,
                    FOREIGN KEY (CredentialProfileId) REFERENCES CredentialProfiles(Id)
                );

                CREATE INDEX IF NOT EXISTS IX_Targets_Hostname ON Targets(Hostname);
                CREATE INDEX IF NOT EXISTS IX_Targets_Status ON Targets(Status);

                -- Deployments table
                CREATE TABLE IF NOT EXISTS Deployments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TargetId INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    Method TEXT,
                    StartTime TEXT,
                    EndTime TEXT,
                    ErrorMessage TEXT,
                    Log TEXT,
                    ConfigurationTemplateId INTEGER,
                    CreatedDate TEXT NOT NULL,
                    FOREIGN KEY (TargetId) REFERENCES Targets(Id),
                    FOREIGN KEY (ConfigurationTemplateId) REFERENCES ConfigurationTemplates(Id)
                );

                CREATE INDEX IF NOT EXISTS IX_Deployments_Status ON Deployments(Status);
                CREATE INDEX IF NOT EXISTS IX_Deployments_TargetId ON Deployments(TargetId);

                -- Jobs table
                CREATE TABLE IF NOT EXISTS Jobs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Type INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    ScheduledTime TEXT NOT NULL,
                    MaintenanceWindowStart TEXT,
                    MaintenanceWindowEnd TEXT,
                    RetryCount INTEGER DEFAULT 0,
                    MaxRetries INTEGER DEFAULT 3,
                    TargetIds TEXT,
                    ConfigData TEXT,
                    CreatedDate TEXT NOT NULL,
                    ModifiedDate TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_Jobs_Status ON Jobs(Status);
                CREATE INDEX IF NOT EXISTS IX_Jobs_ScheduledTime ON Jobs(ScheduledTime);

                -- ConfigurationTemplates table
                CREATE TABLE IF NOT EXISTS ConfigurationTemplates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Description TEXT,
                    ConfigJson TEXT NOT NULL,
                    CreatedDate TEXT NOT NULL,
                    ModifiedDate TEXT NOT NULL
                );

                -- CredentialProfiles table
                CREATE TABLE IF NOT EXISTS CredentialProfiles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Username TEXT NOT NULL,
                    EncryptedPassword TEXT NOT NULL,
                    Type INTEGER NOT NULL,
                    CreatedDate TEXT NOT NULL
                );

                -- HealthChecks table
                CREATE TABLE IF NOT EXISTS HealthChecks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TargetId INTEGER NOT NULL,
                    ServiceStatus TEXT NOT NULL,
                    CPUUsage REAL,
                    MemoryUsageMB INTEGER,
                    DiskSpaceGB INTEGER,
                    ConfigHash TEXT,
                    CheckTime TEXT NOT NULL,
                    FOREIGN KEY (TargetId) REFERENCES Targets(Id)
                );

                CREATE INDEX IF NOT EXISTS IX_HealthChecks_TargetId ON HealthChecks(TargetId);
                CREATE INDEX IF NOT EXISTS IX_HealthChecks_CheckTime ON HealthChecks(CheckTime);
            ";

            using var command = connection.CreateCommand();
            command.CommandText = createTables;
            command.ExecuteNonQuery();
        }

        #region Target Operations

        /// <summary>
        /// Get all targets from database
        /// </summary>
        public List<Target> GetAllTargets()
        {
            var targets = new List<Target>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "SELECT * FROM Targets ORDER BY Hostname";
            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                targets.Add(MapTarget(reader));
            }

            return targets;
        }

        /// <summary>
        /// Get target by ID
        /// </summary>
        public Target? GetTargetById(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "SELECT * FROM Targets WHERE Id = @Id";
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapTarget(reader);
            }

            return null;
        }

        /// <summary>
        /// Add new target to database
        /// </summary>
        public int AddTarget(Target target)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = @"
                INSERT INTO Targets (Hostname, IPAddress, Type, Status, CredentialProfileId,
                                   VertexVersion, LastSeen, CreatedDate, ModifiedDate)
                VALUES (@Hostname, @IPAddress, @Type, @Status, @CredentialProfileId,
                       @VertexVersion, @LastSeen, @CreatedDate, @ModifiedDate);
                SELECT last_insert_rowid();
            ";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Hostname", target.Hostname);
            command.Parameters.AddWithValue("@IPAddress", target.IPAddress ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Type", (int)target.Type);
            command.Parameters.AddWithValue("@Status", (int)target.Status);
            command.Parameters.AddWithValue("@CredentialProfileId", target.CredentialProfileId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@VertexVersion", target.VertexVersion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastSeen", target.LastSeen?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("O"));
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now.ToString("O"));

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Update existing target
        /// </summary>
        public void UpdateTarget(Target target)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = @"
                UPDATE Targets SET
                    Hostname = @Hostname,
                    IPAddress = @IPAddress,
                    Type = @Type,
                    Status = @Status,
                    CredentialProfileId = @CredentialProfileId,
                    VertexVersion = @VertexVersion,
                    LastSeen = @LastSeen,
                    ModifiedDate = @ModifiedDate
                WHERE Id = @Id
            ";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Id", target.Id);
            command.Parameters.AddWithValue("@Hostname", target.Hostname);
            command.Parameters.AddWithValue("@IPAddress", target.IPAddress ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Type", (int)target.Type);
            command.Parameters.AddWithValue("@Status", (int)target.Status);
            command.Parameters.AddWithValue("@CredentialProfileId", target.CredentialProfileId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@VertexVersion", target.VertexVersion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastSeen", target.LastSeen?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now.ToString("O"));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Delete target from database
        /// </summary>
        public void DeleteTarget(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "DELETE FROM Targets WHERE Id = @Id";
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Search targets by hostname
        /// </summary>
        public List<Target> SearchTargets(string query)
        {
            var targets = new List<Target>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT * FROM Targets WHERE Hostname LIKE @Query ORDER BY Hostname";
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@Query", $"%{query}%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                targets.Add(MapTarget(reader));
            }

            return targets;
        }

        private Target MapTarget(SqliteDataReader reader)
        {
            return new Target
            {
                Id = reader.GetInt32(0),
                Hostname = reader.GetString(1),
                IPAddress = reader.IsDBNull(2) ? null : reader.GetString(2),
                Type = (TargetType)reader.GetInt32(3),
                Status = (TargetStatus)reader.GetInt32(4),
                CredentialProfileId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                VertexVersion = reader.IsDBNull(6) ? null : reader.GetString(6),
                LastSeen = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                CreatedDate = DateTime.Parse(reader.GetString(8)),
                ModifiedDate = DateTime.Parse(reader.GetString(9))
            };
        }

        #endregion

        #region Deployment Operations

        /// <summary>
        /// Create new deployment record
        /// </summary>
        public int CreateDeployment(Deployment deployment)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = @"
                INSERT INTO Deployments (TargetId, Status, Method, StartTime, EndTime,
                                       ErrorMessage, Log, ConfigurationTemplateId, CreatedDate)
                VALUES (@TargetId, @Status, @Method, @StartTime, @EndTime,
                       @ErrorMessage, @Log, @ConfigurationTemplateId, @CreatedDate);
                SELECT last_insert_rowid();
            ";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@TargetId", deployment.TargetId);
            command.Parameters.AddWithValue("@Status", (int)deployment.Status);
            command.Parameters.AddWithValue("@Method", deployment.Method ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@StartTime", deployment.StartTime?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EndTime", deployment.EndTime?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ErrorMessage", deployment.ErrorMessage ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Log", deployment.Log ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ConfigurationTemplateId", deployment.ConfigurationTemplateId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("O"));

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Update deployment status
        /// </summary>
        public void UpdateDeploymentStatus(int id, DeploymentStatus status, string? errorMessage = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = @"
                UPDATE Deployments SET
                    Status = @Status,
                    ErrorMessage = @ErrorMessage,
                    EndTime = @EndTime
                WHERE Id = @Id
            ";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Status", (int)status);
            command.Parameters.AddWithValue("@ErrorMessage", errorMessage ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EndTime",
                (status == DeploymentStatus.Success || status == DeploymentStatus.Failed)
                    ? DateTime.Now.ToString("O")
                    : (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Update deployment log
        /// </summary>
        public void UpdateDeploymentLog(int id, string log)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "UPDATE Deployments SET Log = @Log WHERE Id = @Id";
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Log", log);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Get deployments by status
        /// </summary>
        public List<Deployment> GetDeploymentsByStatus(DeploymentStatus status)
        {
            var deployments = new List<Deployment>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "SELECT * FROM Deployments WHERE Status = @Status ORDER BY CreatedDate DESC";
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Status", (int)status);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                deployments.Add(MapDeployment(reader));
            }

            return deployments;
        }

        /// <summary>
        /// Get all deployments for a specific target
        /// </summary>
        public List<Deployment> GetDeploymentsForTarget(int targetId)
        {
            var deployments = new List<Deployment>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = targetId == 0
                ? "SELECT * FROM Deployments ORDER BY CreatedDate DESC"
                : "SELECT * FROM Deployments WHERE TargetId = @TargetId ORDER BY CreatedDate DESC";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            if (targetId > 0)
                command.Parameters.AddWithValue("@TargetId", targetId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                deployments.Add(MapDeployment(reader));
            }

            return deployments;
        }

        private Deployment MapDeployment(SqliteDataReader reader)
        {
            return new Deployment
            {
                Id = reader.GetInt32(0),
                TargetId = reader.GetInt32(1),
                Status = (DeploymentStatus)reader.GetInt32(2),
                Method = reader.IsDBNull(3) ? null : reader.GetString(3),
                StartTime = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                EndTime = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6),
                Log = reader.IsDBNull(7) ? null : reader.GetString(7),
                ConfigurationTemplateId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                CreatedDate = DateTime.Parse(reader.GetString(9))
            };
        }

        #endregion

        #region Credential Profile Operations

        /// <summary>
        /// Save credential profile
        /// </summary>
        public int SaveCredentialProfile(CredentialProfile profile)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = @"
                INSERT INTO CredentialProfiles (Name, Username, EncryptedPassword, Type, CreatedDate)
                VALUES (@Name, @Username, @EncryptedPassword, @Type, @CreatedDate);
                SELECT last_insert_rowid();
            ";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Name", profile.Name);
            command.Parameters.AddWithValue("@Username", profile.Username);
            command.Parameters.AddWithValue("@EncryptedPassword", profile.EncryptedPassword);
            command.Parameters.AddWithValue("@Type", (int)profile.Type);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("O"));

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Get credential profile by ID
        /// </summary>
        public CredentialProfile? GetCredentialProfile(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "SELECT * FROM CredentialProfiles WHERE Id = @Id";
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new CredentialProfile
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Username = reader.GetString(2),
                    EncryptedPassword = reader.GetString(3),
                    Type = (CredentialType)reader.GetInt32(4),
                    CreatedDate = DateTime.Parse(reader.GetString(5))
                };
            }

            return null;
        }

        /// <summary>
        /// Get all credential profiles
        /// </summary>
        public List<CredentialProfile> GetAllCredentialProfiles()
        {
            var profiles = new List<CredentialProfile>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "SELECT * FROM CredentialProfiles ORDER BY Name";
            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                profiles.Add(new CredentialProfile
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Username = reader.GetString(2),
                    EncryptedPassword = reader.GetString(3),
                    Type = (CredentialType)reader.GetInt32(4),
                    CreatedDate = DateTime.Parse(reader.GetString(5))
                });
            }

            return profiles;
        }

        #endregion

        // Additional methods for Jobs, ConfigurationTemplates, and HealthChecks
        // can be added here following the same pattern...

        public string GetDatabasePath() => _databasePath;
    }
}
