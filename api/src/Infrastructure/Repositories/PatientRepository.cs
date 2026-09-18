using System.Data;
using Application.Interfaces;
using Dapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PatientRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> RegisterAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@FullName", patient.FullName, DbType.String, ParameterDirection.Input, 200);
        parameters.Add("@DocumentType", patient.DocumentType, DbType.String, ParameterDirection.Input, 10);
        parameters.Add("@DocumentNumber", patient.DocumentNumber, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@CountryCode", patient.CountryCode, DbType.AnsiStringFixedLength, ParameterDirection.Input, 2);
        parameters.Add("@Phone", patient.Phone, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@Email", patient.Email, DbType.String, ParameterDirection.Input, 150);
        parameters.Add("@City", patient.City, DbType.String, ParameterDirection.Input, 100);
        parameters.Add("@TreatmentStart", patient.TreatmentStart, DbType.Date, ParameterDirection.Input);
        parameters.Add("@FollowUpDays", patient.FollowUpDays, DbType.Int32, ParameterDirection.Input);
        parameters.Add("@ConsentDate", patient.ConsentDate, DbType.Date, ParameterDirection.Input);
        parameters.Add("@CreatedBy", patient.CreatedBy, DbType.Guid, ParameterDirection.Input);
        parameters.Add("@NewPatientId", dbType: DbType.Guid, direction: ParameterDirection.Output);

        try
        {
            var command = new CommandDefinition(
                "dbo.sp_RegisterPatient",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            return parameters.Get<Guid>("@NewPatientId");
        }
        catch (SqlException ex) when (ex.Number is 50001 or 50004 or 50005)
        {
            throw new ConflictException(ex.Message);
        }
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PatientId", id, DbType.Guid);

        var command = new CommandDefinition(
            "dbo.sp_GetPatientById",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var record = await connection.QuerySingleOrDefaultAsync<PatientRecord>(command);
        if (record == null) return null;

        return MapRecordToEntity(record);
    }

    public async Task<(IEnumerable<Patient> Items, int TotalCount)> GetPagedAsync(string? countryCode, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@CountryCode", string.IsNullOrWhiteSpace(countryCode) ? null : countryCode, DbType.AnsiStringFixedLength, ParameterDirection.Input, 2);
        parameters.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@PageNumber", pageNumber < 1 ? 1 : pageNumber, DbType.Int32);
        parameters.Add("@PageSize", pageSize < 1 ? 20 : pageSize, DbType.Int32);

        var command = new CommandDefinition(
            "dbo.sp_GetPatients",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var records = (await connection.QueryAsync<PatientRecordWithCount>(command)).ToList();
        var totalCount = records.FirstOrDefault()?.total_count ?? 0;
        var patients = records.Select(MapRecordToEntity);

        return (patients, totalCount);
    }

    public async Task UpdateWithAuditAsync(Guid patientId, string phone, string city, int followUpDays, string status, Guid changedBy, string reason, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PatientId", patientId, DbType.Guid);
        parameters.Add("@Phone", phone, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@City", city, DbType.String, ParameterDirection.Input, 100);
        parameters.Add("@FollowUpDays", followUpDays, DbType.Int32);
        parameters.Add("@Status", status, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@ChangedBy", changedBy, DbType.Guid);
        parameters.Add("@Reason", reason, DbType.String, ParameterDirection.Input, 300);

        try
        {
            var command = new CommandDefinition(
                "dbo.sp_UpdatePatientWithAudit",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex) when (ex.Number is 50050 or 50020)
        {
            throw new ValidationException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 50030 or 50002)
        {
            throw new NotFoundException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 50005)
        {
            throw new ConflictException(ex.Message);
        }
    }

    public async Task<IEnumerable<PatientAuditLog>> GetAuditHistoryAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PatientId", patientId, DbType.Guid);

        var command = new CommandDefinition(
            "dbo.sp_GetPatientAuditHistory",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var records = await connection.QueryAsync<PatientAuditRecord>(command);
        return records.Select(r => new PatientAuditLog
        {
            Id = r.id,
            PatientId = r.patient_id,
            ChangedBy = r.changed_by,
            ChangedByName = r.changed_by_name,
            ChangedAt = r.changed_at,
            Reason = r.reason,
            PreviousValue = r.previous_value,
            NewValue = r.new_value
        });
    }

    private static Patient MapRecordToEntity(PatientRecord r)
    {
        return new Patient
        {
            Id = r.id,
            FullName = r.full_name,
            DocumentType = r.document_type,
            DocumentNumber = r.document_number,
            CountryCode = r.country_code,
            Phone = r.phone,
            Email = r.email,
            City = r.city,
            TreatmentStart = r.treatment_start,
            FollowUpDays = r.follow_up_days,
            Status = r.status,
            RegistrationSource = r.registration_source,
            ConsentDate = r.consent_date,
            CreatedBy = r.created_by,
            CreatedAt = r.created_at,
            UpdatedAt = r.updated_at
        };
    }

    private class PatientRecord
    {
        public Guid id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string? document_type { get; set; }
        public string? document_number { get; set; }
        public string country_code { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string email { get; set; } = string.Empty;
        public string? city { get; set; }
        public DateTime? treatment_start { get; set; }
        public int? follow_up_days { get; set; }
        public string status { get; set; } = string.Empty;
        public string registration_source { get; set; } = string.Empty;
        public DateTime? consent_date { get; set; }
        public Guid created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    private sealed class PatientRecordWithCount : PatientRecord
    {
        public int total_count { get; set; }
    }

    private sealed class PatientAuditRecord
    {
        public Guid id { get; set; }
        public Guid patient_id { get; set; }
        public Guid changed_by { get; set; }
        public string changed_by_name { get; set; } = string.Empty;
        public DateTime changed_at { get; set; }
        public string reason { get; set; } = string.Empty;
        public string previous_value { get; set; } = string.Empty;
        public string new_value { get; set; } = string.Empty;
    }
}
