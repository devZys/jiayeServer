using FluentNHibernate.Mapping;

namespace LegendServer.Database.ServerDeploy
{
    //服务器部署表
    public class ServerDeployConfigDB
    {
        public virtual string name { get; set; }
        public virtual int id { get; set; }
        public virtual string ip { get; set; }
        public virtual int tcp_port { get; set; }
        public virtual string remark { get; set; }

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
            ServerDeployConfigDB serverDeployDB = obj as ServerDeployConfigDB;
            if (!this.name.Equals(serverDeployDB.name) || this.id != serverDeployDB.id)
            {
                return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return this.name.GetHashCode() + this.id.GetHashCode() + this.tcp_port.GetHashCode();
        }

    }
    class ServerDeployDBMapping : ClassMap<ServerDeployConfigDB>
    {
        public ServerDeployDBMapping()
        {
            CompositeId()
           .KeyProperty(x => x.name, "name")
           .KeyProperty(x => x.id, "id");
            Map(x => x.ip).Column("ip");
            Map(x => x.tcp_port).Column("tcp_port");
            Map(x => x.remark).Column("remark");

            Table("serverdeployconfig");
        }
    }
}
