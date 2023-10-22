using MCTools.SDK.Models;

namespace MCTools.SDK.Interfaces.Controllers
{
	public interface IEditionController : IController
	{
		Task<List<MCVersion>> GetVersions();

		Task<MCAssets> GetAssets(string version);
	}
}
