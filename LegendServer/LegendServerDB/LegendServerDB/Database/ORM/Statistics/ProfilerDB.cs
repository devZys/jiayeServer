using FluentNHibernate.Mapping;

namespace LegendServer.Database.Profiler
{
    public class ProfilerDB
    {
        public virtual string serverName { get; set; }
        public virtual int serverID { get; set; }
        public virtual string name { get; set; }
        public virtual long timeElapsed { get; set; }
        public virtual long cpuCycles { get; set; }
        public virtual string gcGeneration { get; set; }
        public virtual long callCount { get; set; }
        public virtual int msgSize { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            ProfilerDB profilerDB = obj as ProfilerDB;
            if (!this.serverName.Equals(profilerDB.serverName) || this.serverID != profilerDB.serverID || !this.name.Equals(profilerDB.name))
            {
                return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return this.serverName.GetHashCode() + this.serverID.GetHashCode() + this.name.GetHashCode();
        }
    }
    class ProfilerDBMapping : ClassMap<ProfilerDB>
    {
        public ProfilerDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.serverName, "serverName")
            .KeyProperty(x => x.serverID, "serverID")
            .KeyProperty(x => x.name, "name");
            Map(x => x.timeElapsed).Column("timeElapsed");
            Map(x => x.cpuCycles).Column("cpuCycles");
            Map(x => x.gcGeneration).Column("gcGeneration");
            Map(x => x.callCount).Column("callCount");
            Map(x => x.msgSize).Column("msgSize");

            Table("profiler");
        }
    }
}
