using System.Data;
using Application.Interfaces;
using Dapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ContactRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreateAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PatientId", contact.PatientId, DbType.Guid);
        parameters.Add("@ContactDate", contact.ContactDate, DbType.DateTime2);
        parameters.Add("@Channel", contact.Channel, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@Result", contact.Result, DbType.String, ParameterDirection.Input, 30);
        parameters.Add("@Notes", contact.Notes, DbType.String, ParameterDirection.Input, 500);
        parameters.Add("@RegisteredBy", contact.RegisteredBy, DbType.Guid);
        parameters.Add("@NewContactId", dbType: DbType.Guid, direction: ParameterDirection.Output);

        try
        {
            var command = new CommandDefinition(
                "dbo.sp_CreateContact",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            return parameters.Get<Guid>("@NewContactId");
        }
        catch (SqlException ex) when (ex.Number == 50030)
        {
            throw new NotFoundException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 50031)
        {
            throw new UnprocessableEntityException(ex.Message);
        }
    }

    public async Task<IEnumerable<Contact>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PatientId", patientId, DbType.Guid);

        var command = new CommandDefinition(
            "dbo.sp_GetContactsByPatient",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var records = await connection.QueryAsync<ContactRecord>(command);
        return records.Select(r => new Contact
        {
            Id = r.id,
            PatientId = r.patient_id,
            ContactDate = r.contact_date,
            Channel = r.channel,
            Result = r.result,
            Notes = r.notes,
            IsActive = r.is_active,
            RegisteredBy = r.registered_by,
            RegisteredByName = r.registered_by_name,
            CreatedAt = r.created_at
        });
    }

    public async Task<Guid> CorrectAsync(Guid originalContactId, Guid changedBy, DateTime newDate, string newChannel, string newResult, string? newNotes, string reason, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@OriginalContactId", originalContactId, DbType.Guid);
        parameters.Add("@ChangedBy", changedBy, DbType.Guid);
        parameters.Add("@NewContactDate", newDate, DbType.DateTime2);
        parameters.Add("@NewChannel", newChannel, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@NewResult", newResult, DbType.String, ParameterDirection.Input, 30);
        parameters.Add("@NewNotes", newNotes, DbType.String, ParameterDirection.Input, 500);
        parameters.Add("@Reason", reason, DbType.String, ParameterDirection.Input, 300);
        parameters.Add("@NewContactId", dbType: DbType.Guid, direction: ParameterDirection.Output);

        try
        {
            var command = new CommandDefinition(
                "dbo.sp_CorrectContact",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            return parameters.Get<Guid>("@NewContactId");
        }
        catch (SqlException ex) when (ex.Number == 50040)
        {
            throw new ValidationException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 50041)
        {
            throw new NotFoundException(ex.Message);
        }
    }

    public async Task<IEnumerable<ContactAuditLog>> GetAuditByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PatientId", patientId, DbType.Guid);

        var command = new CommandDefinition(
            "dbo.sp_GetContactAuditByPatient",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var records = await connection.QueryAsync<ContactAuditRecord>(command);
        return records.Select(r => new ContactAuditLog
        {
            Id = r.id,
            ContactId = r.contact_id,
            ChangedBy = r.changed_by,
            ChangedByName = r.changed_by_name,
            ChangedAt = r.changed_at,
            Reason = r.reason,
            PreviousValue = r.previous_value,
            NewValue = r.new_value
        });
    }

    public async Task<IEnumerable<ContactAuditLog>> GetAuditByContactIdAsync(Guid contactId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@ContactId", contactId, DbType.Guid);

        var command = new CommandDefinition(
            "dbo.sp_GetContactAuditHistory",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var records = await connection.QueryAsync<ContactAuditRecord>(command);
        return records.Select(r => new ContactAuditLog
        {
            Id = r.id,
            ContactId = r.contact_id,
            ChangedBy = r.changed_by,
            ChangedByName = r.changed_by_name,
            ChangedAt = r.changed_at,
            Reason = r.reason,
            PreviousValue = r.previous_value,
            NewValue = r.new_value
        });
    }

    private sealed class ContactRecord
    {
        public Guid id { get; set; }
        public Guid patient_id { get; set; }
        public DateTime contact_date { get; set; }
        public string channel { get; set; } = string.Empty;
        public string result { get; set; } = string.Empty;
        public string? notes { get; set; }
        public bool is_active { get; set; }
        public Guid registered_by { get; set; }
        public string registered_by_name { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
    }

    private sealed class ContactAuditRecord
    {
        public Guid id { get; set; }
        public Guid contact_id { get; set; }
        public Guid changed_by { get; set; }
        public string changed_by_name { get; set; } = string.Empty;
        public DateTime changed_at { get; set; }
        public string reason { get; set; } = string.Empty;
        public string previous_value { get; set; } = string.Empty;
        public string new_value { get; set; } = string.Empty;
    }
}
