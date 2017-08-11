using System.Collections;
using LegendProtocol;

//登陆主对象
public class LoginMain : Module
{
    public LoginMsgServerProxy server_msg_proxy;//服务器消息代理
    public LoginDbProxy db_proxy;//数据库代理

    public override void Init()
    {
        client_msg_proxy = new LoginMsgClientProxy(this);
        server_msg_proxy = new LoginMsgServerProxy(this);
        view_proxy = new LoginViewProxy(this);
        db_proxy = new LoginDbProxy(this);
    }

    public override void RegistMsg()
    {
        //服务器消息句柄注册
        MsgRegister.Instance().RegistServerHandle(MsgID.C2G_RequestLogin, server_msg_proxy.OnReqLogin);

        //客户机消息句柄注册
        MsgRegister.Instance().RegistClientHandle(MsgID.G2C_ReplayLogin, client_msg_proxy.OnReplayLogin);
    }

    public override void Start()
    {
        client_msg_proxy.Start();
        server_msg_proxy.Start();
        view_proxy.Start();
        db_proxy.Start();
	}
    public override void Update(float time)
    {
    }

    public override void Destory()
    {
        client_msg_proxy.Destory();
        server_msg_proxy.Destroy();
        view_proxy.Destory();
        db_proxy.Destory();
    }
    public override void SaveDB()
    {
    }

    public void TryLogin(string accountID, string password)
    {
        client_msg_proxy.RequestLogin(accountID, password);
    }

}
