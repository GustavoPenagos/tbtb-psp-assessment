using System.Data;
using Application.Interfaces;
using Dapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Repositories;

public class RegistrationLinkRepository : IRegistrationLinkRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RegistrationLinkRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(string Token, DateTime ExpiresAt)> CreateAsync(Guid createdBy, int expiresInDays, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@CreatedBy", createdBy, DbType.Guid);
        parameters.Add("@ExpiresInDays", expiresInDays, DbType.Int32);
        parameters.Add("@Token", dbType: DbType.String, direction: ParameterDirection.Output, size: 64);
        parameters.Add("@ExpiresAt", dbType: DbType.DateTime2, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "dbo.sp_CreateRegistrationLink",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        var token = parameters.Get<string>("@Token");
        var expiresAt = parameters.Get<DateTime>("@ExpiresAt");
        return (token, expiresAt);
    }

    public async Task<RegistrationLink?> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Token", token, DbType.String, ParameterDirection.Input, 64);

        var command = new CommandDefinition(
            "dbo.sp_ValidateRegistrationLink",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var record = await connection.QuerySingleOrDefaultAsync<RegistrationLinkRecord>(command);
        if (record == null) return null;

        return new RegistrationLink
        {
            Id = record.id,
            Token = record.token,
            CreatedBy = record.created_by,
            PatientId = record.patient_id,
            Status = record.computed_status,
            ExpiresAt = record.expires_at,
            CreatedAt = record.created_at,
            UsedAt = record.used_at
        };
    }

    public async Task<(Guid PatientId, string SessionToken)> IdentifyPatientAsync(string token, string fullName, string email, string countryCode, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Token", token, DbType.String, ParameterDirection.Input, 64);
        parameters.Add("@FullName", fullName, DbType.String, ParameterDirection.Input, 200);
        parameters.Add("@Email", email, DbType.String, ParameterDirection.Input, 150);
        parameters.Add("@CountryCode", countryCode, DbType.AnsiStringFixedLength, ParameterDirection.Input, 2);
        parameters.Add("@PatientId", dbType: DbType.Guid, direction: ParameterDirection.Output);
        parameters.Add("@SessionToken", dbType: DbType.String, direction: ParameterDirection.Output, size: 64);

        try
        {
            var command = new CommandDefinition(
                "dbo.sp_IdentifyPatientSelfReg",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            var patientId = parameters.Get<Guid>("@PatientId");
            var sessionToken = parameters.Get<string>("@SessionToken");
            return (patientId, sessionToken);
        }
        catch (SqlException ex) when (ex.Number == 50010)
        {
            throw new NotFoundException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 50011 or 50004)
        {
            throw new ConflictException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 50012)
        {
            throw new ValidationException(ex.Message);
        }
    }

    public async Task<Guid> CompletePatientAsync(string token, string docType, string docNumber, string phone, string city, DateTime treatmentStart, int followUpDays, DateTime? consentDate, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Token", token, DbType.String, ParameterDirection.Input, 64);
        parameters.Add("@DocumentType", docType, DbType.String, ParameterDirection.Input, 10);
        parameters.Add("@DocumentNumber", docNumber, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@Phone", phone, DbType.String, ParameterDirection.Input, 20);
        parameters.Add("@City", city, DbType.String, ParameterDirection.Input, 100);
        parameters.Add("@TreatmentStart", treatmentStart, DbType.Date);
        parameters.Add("@FollowUpDays", followUpDays, DbType.Int32);
        parameters.Add("@ConsentDate", consentDate, DbType.Date);
        parameters.Add("@PatientId", dbType: DbType.Guid, direction: ParameterDirection.Output);

        try
        {
            var command = new CommandDefinition(
                "dbo.sp_CompletePatientSelfReg",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            return parameters.Get<Guid>("@PatientId");
        }
        catch (SqlException ex) when (ex.Number is 50020 or 50021 or 50022)
        {
            throw new ValidationException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 50001 or 50005)
        {
            throw new ConflictException(ex.Message);
        }
    }

    private sealed class RegistrationLinkRecord
    {
        public Guid id { get; set; }
        public string token { get; set; } = string.Empty;
        public Guid created_by { get; set; }
        public Guid? patient_id { get; set; }
        public string status { get; set; } = string.Empty;
        public string computed_status { get; set; } = string.Empty;
        public DateTime expires_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? used_at { get; set; }
    }
}
