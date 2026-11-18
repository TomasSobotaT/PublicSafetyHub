using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using PublicSafetyHub.Base.Entities.Base;

namespace PublicSafetyHub.Data.Conventions;

public class DateTimeConvention : IPropertyAddedConvention
{
    public void ProcessPropertyAdded(IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
    {
        var property = propertyBuilder.Metadata;
        var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

        if (propertyType == typeof(DateTime) && typeof(IBaseEntity).IsAssignableFrom(property.DeclaringType.ClrType))
        {
            propertyBuilder.HasColumnType("timestamp with time zone");
        }
    }
}
