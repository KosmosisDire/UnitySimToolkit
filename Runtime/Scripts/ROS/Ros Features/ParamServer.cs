using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RosMessageTypes.Rosapi;

public class ParamServer : RosFeatureSingleton<ParamServer>
{
    const string GET_PARAM = "rosapi/get_param";
    const string SET_PARAM = "rosapi/set_param";

    protected override async Task Init()
    {
        await base.Init();
        ROS.RegisterRosService<GetParamRequest, GetParamResponse>(GET_PARAM);
        ROS.RegisterRosService<SetParamRequest, SetParamResponse>(SET_PARAM);
    }

    public static async Task<string> GetParam(string key, string @default = "")
    {
        var response = (await ROS.SendServiceMessage<GetParamResponse>(GET_PARAM, new GetParamRequest(key, @default))).value;
        response = Regex.Unescape(response).Trim('"');
        return response;
    }

    public static async Task SetParam(string key, string value)
    {
        await ROS.SendServiceMessage<SetParamResponse>(SET_PARAM, new SetParamRequest(key, value));
    }

}