using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using BTCPayServer.HostedServices;
using BTCPayServer.Security;
using BTCPayServer.Services;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Controllers.GreenField
{
    [ApiController]
    public class GreenFieldServerInfoController : Controller
    {
        private readonly BTCPayServerEnvironment _env;
        private readonly NBXplorerDashboard _dashBoard;
        private readonly StoreRepository _storeRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BTCPayNetworkProvider _networkProvider;
        private readonly PaymentMethodHandlerDictionary _paymentMethodHandlerDictionary;

        public GreenFieldServerInfoController(
            BTCPayServerEnvironment env, 
            NBXplorerDashboard dashBoard, 
            StoreRepository storeRepository, 
            UserManager<ApplicationUser> userManager,
            BTCPayNetworkProvider networkProvider,
            PaymentMethodHandlerDictionary paymentMethodHandlerDictionary)
        {
            _env = env;
            _dashBoard = dashBoard;
            _storeRepository = storeRepository;
            _userManager = userManager;
            _networkProvider = networkProvider;
            _paymentMethodHandlerDictionary = paymentMethodHandlerDictionary;
        }
        
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpGet("~/api/v1/server/info")]
        public async Task<ActionResult> ServerInfo()
        {
            var stores = await _storeRepository.GetStoresByUserId(_userManager.GetUserId(User));
            var supportedPaymentMethods = _paymentMethodHandlerDictionary
                .SelectMany(handler => handler.GetSupportedPaymentMethods().Select(id => id.ToString()))
                .Distinct();
            var syncStatus = _dashBoard.GetAll()
                .Select(summary => new ServerInfoSyncStatusData
                {
                    CryptoCode = summary.Network.CryptoCode,
                    BlockHeaders = summary.Status.ChainHeight, 
                    Progress = summary.Status.SyncHeight.GetValueOrDefault(0) / (float)summary.Status.ChainHeight
                });
            ServerInfoStatusData status = new ServerInfoStatusData
            {
                FullySynched = _dashBoard.IsFullySynched(),
                SyncStatus = syncStatus
            };
            ServerInfoData model = new ServerInfoData
            {
                Status = status,
                Onion = _env.OnionUrl,
                Version = _env.Version,
                SupportedPaymentMethods = supportedPaymentMethods
            };
            return Ok(model);
        }
    }
}
