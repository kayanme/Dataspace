using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Statistics.Events;

namespace Dataspace.Loggers.Mappers
{

    internal class LogContext:DbContext
    {
        public LogContext():base("name=DataspaceLogger")
        {
            
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<StatisticEvent>().HasKey(k=>k.EventKey);
            modelBuilder.Entity<CachedGetEvent>().Map(k =>
                                                          {
                                                              k.ToTable("CachedGetEvent");
                                                              k.MapInheritedProperties();
                                                          });
            modelBuilder.Entity<ExternalGetEvent>().Map(k =>
            {
                k.ToTable("ExternalGetEvent");
                k.MapInheritedProperties();
            });
            modelBuilder.Entity<ExternalSerialGetEvent>().Map(k =>
            {
                k.ToTable("ExternalSerialGetEvent");
                k.MapInheritedProperties();
            });
            modelBuilder.Entity<PostEvent>().Map(k =>
            {
                k.ToTable("PostEvent");
                k.MapInheritedProperties();
            });
            modelBuilder.Entity<RebalanceEvent>().Map(k =>
            {
                k.ToTable("RebalanceEvent");
                k.MapInheritedProperties();
            });
            modelBuilder.Entity<UnactualGetEvent>().Map(k =>
            {
                k.ToTable("UnactualGetEvent");
                k.MapInheritedProperties();
            });
        }
    }
}
