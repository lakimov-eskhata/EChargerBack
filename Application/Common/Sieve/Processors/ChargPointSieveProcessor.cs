using Domain.Entities.Station;
using Sieve.Services;

namespace Application.Common.Sieve.Processors;

public class ChargPointSieveProcessor: ISieveConfiguration
{
    public void Configure(SievePropertyMapper mapper)
    {
        // Настраиваем маппинг для ChargePoint
        mapper.Property<ChargePointEntity>(cp => cp.ChargePointId)
            .CanSort()
            .CanFilter()
            .HasName("chargePointId");

        mapper.Property<ChargePointEntity>(cp => cp.Name)
            .CanSort()
            .CanFilter()
            .HasName("name");

        mapper.Property<ChargePointEntity>(cp => cp.CreatedAt)
            .CanSort()
            .HasName("createdAt");
    }
}