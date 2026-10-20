using System.Data;
using System.Data.Common;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FarmManagement.Infrastructure.Services;

/// <summary>
/// Concurrency-safe, atomic reference numbering generator backed by the fertilizer_reference_sequences table.
/// Allocates formatted reference numbers: FERT-YYYY-NNNNNN scoped per organization and calendar year.
/// </summary>
public sealed class FertilizerReferenceGenerator(ApplicationDbContext dbContext) : IFertilizerReferenceGenerator
{
    private static readonly SemaphoreSlim InProcessLock = new(1, 1);

    public async Task<string> NextReferenceNumberAsync(Guid organizationId, int year, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization identifier is required.", nameof(organizationId));
        }

        if (year < 2000 || year > 2200)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2200.");
        }

        var isPostgres = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        if (isPostgres)
        {
            // In PostgreSQL, execute an atomic upsert statement with RETURNING last_number.
            // This is concurrency-safe without race conditions or sequence gaps under parallel requests.
            var now = DateTimeOffset.UtcNow;
            var connection = dbContext.Database.GetDbConnection();
            var shouldCloseConnection = false;
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
                shouldCloseConnection = true;
            }

            try
            {
                await using var command = connection.CreateCommand();
                const string sql = """
                    INSERT INTO fertilizer_reference_sequences (organization_id, year, last_number, updated_at)
                    VALUES (@p_org_id, @p_year, 1, @p_now)
                    ON CONFLICT (organization_id, year)
                    DO UPDATE SET last_number = fertilizer_reference_sequences.last_number + 1, updated_at = @p_now
                    RETURNING last_number;
                    """;

                command.CommandText = sql;
                var currentTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction;
                }

                command.Parameters.Add(new NpgsqlParameter("p_org_id", organizationId));
                command.Parameters.Add(new NpgsqlParameter("p_year", year));
                command.Parameters.Add(new NpgsqlParameter("p_now", now));

                var result = await command.ExecuteScalarAsync(cancellationToken);
                var nextNumber = Convert.ToInt32(result);

                return $"FERT-{year}-{nextNumber:D6}";
            }
            finally
            {
                if (shouldCloseConnection && dbContext.Database.CurrentTransaction == null)
                {
                    await connection.CloseAsync();
                }
            }
        }

        // In-memory or SQLite fallback for test suites
        await InProcessLock.WaitAsync(cancellationToken);
        try
        {
            var seq = await dbContext.FertilizerReferenceSequences
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Year == year, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            int nextNumber;
            if (seq is null)
            {
                seq = new FertilizerReferenceSequence(organizationId, year, 1);
                dbContext.FertilizerReferenceSequences.Add(seq);
                nextNumber = 1;
            }
            else
            {
                nextNumber = seq.NextNumber(now);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return $"FERT-{year}-{nextNumber:D6}";
        }
        finally
        {
            InProcessLock.Release();
        }
    }
}
