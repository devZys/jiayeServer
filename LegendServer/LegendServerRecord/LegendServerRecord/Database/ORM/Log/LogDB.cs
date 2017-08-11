using FluentNHibernate.Mapping;

namespace LegendServer.Database.Log
{
    public class LogInfoDB
    {
        public virtual ulong logID { get; set; }
        public virtual string serverName { get; set; }
        public virtual int serverID { get; set; }
        public virtual string context { get; set; }
    }
    class LogInfoDBMapping : ClassMap<LogInfoDB>
    {
        public LogInfoDBMapping()
        {
            Id(x => x.logID).Column("logID").GeneratedBy.Assigned();
            Map(x => x.serverName).Column("serverName");
            Map(x => x.serverID).Column("serverID");
            Map(x => x.context).Column("context");

            Table("loginfo");
        }
    }
    public class LogDebugDB
    {
        public virtual ulong logID { get; set; }
        public virtual string serverName { get; set; }
        public virtual int serverID { get; set; }
        public virtual string context { get; set; }
    }
    class LogDebugDBMapping : ClassMap<LogDebugDB>
    {
        public LogDebugDBMapping()
        {
            Id(x => x.logID).Column("logID").GeneratedBy.Assigned();
            Map(x => x.serverName).Column("serverName");
            Map(x => x.serverID).Column("serverID");
            Map(x => x.context).Column("context");

            Table("logdebug");
        }
    }
    public class LogErrorDB
    {
        public virtual ulong logID { get; set; }
        public virtual string serverName { get; set; }
        public virtual int serverID { get; set; }
        public virtual string context { get; set; }
    }
    class LogErrorDBMapping : ClassMap<LogErrorDB>
    {
        public LogErrorDBMapping()
        {
            Id(x => x.logID).Column("logID").GeneratedBy.Assigned();
            Map(x => x.serverName).Column("serverName");
            Map(x => x.serverID).Column("serverID");
            Map(x => x.context).Column("context");

            Table("logerror");
        }
    }
    public class LogFatalDB
    {
        public virtual ulong logID { get; set; }
        public virtual string serverName { get; set; }
        public virtual int serverID { get; set; }
        public virtual string context { get; set; }
    }
    class LogFatalDBMapping : ClassMap<LogFatalDB>
    {
        public LogFatalDBMapping()
        {
            Id(x => x.logID).Column("logID").GeneratedBy.Assigned();
            Map(x => x.serverName).Column("serverName");
            Map(x => x.serverID).Column("serverID");
            Map(x => x.context).Column("context");

            Table("logfatal");
        }
    }
}
