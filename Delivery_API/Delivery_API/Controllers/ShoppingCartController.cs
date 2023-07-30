using Delivery_API.DbContexts;
using Delivery_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Delivery_API.Controllers
{
    [Route("api/shoppingCart")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        protected ApiResponse _response;
        private readonly ApplicationDbContexts _db;
        public ShoppingCartController(ApplicationDbContexts db)
        {
            _response = new();
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetShoppingCart(string userId)
        {
            try
            {
                ShoppingCart shoppingCart;

                if(string.IsNullOrEmpty(userId))
                {
                    shoppingCart = new();
                }
                else
                {
                    shoppingCart = _db.ShoppingCarts
                        .Include(u => u.CartItems)
                        .ThenInclude(u => u.MenuItem)
                        .FirstOrDefault(u => u.UserId == userId);
                }

                if(shoppingCart.CartItems != null && shoppingCart.CartItems.Count > 0)
                {
                    shoppingCart.CartTotal = shoppingCart.CartItems
                        .Sum(u => u.Quantity * u.MenuItem.Price);
                }

                _response.Result = shoppingCart;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch(Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                _response.StatusCode = HttpStatusCode.BadRequest;
            }
            return _response;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> AddOrUpdateItemInCart(string userId, int menuItemId, int updateQuantityBy)
        {
            //Korpa ce imati jedan unos po id-u korisnika, cak iako korisnik ima vise stavki u korpi
            //CartItems ce imati sve stavke u korpi(ShoppingCart) za jedinstvenog korisnika
            //updateQuantityBy ce cuvati kolicinu po kojoj zelimo da azuriramo kvantitet proizvoda
            //Ukoliko je -1, znaci da smo smanjili kolicinu, a ukoliko je 0, stavka ce biti obrisana iz korpe

            ShoppingCart shoppingCart = _db.ShoppingCarts.Include(u => u.CartItems)
                .FirstOrDefault(u => u.UserId == userId);
            MenuItem menuItem = _db.MenuItems.FirstOrDefault(u => u.Id == menuItemId);
            if(menuItem == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            if(shoppingCart == null && updateQuantityBy > 0)
            {
                //Kreiram korpu i dodajem CartItem
                ShoppingCart newCart = new() { UserId = userId };
                _db.ShoppingCarts.Add(newCart);
                _db.SaveChanges();

                CartItem newCartItem = new()
                {
                    MenuItemId = menuItemId,
                    Quantity = updateQuantityBy,
                    ShoppingCartId = newCart.Id,
                    MenuItem = null
                };
                _db.CartItems.Add(newCartItem);
                _db.SaveChanges();
            }
            else
            {
                //Ukoliko shopping cart postoji

                CartItem cartItemCart = shoppingCart.CartItems.FirstOrDefault(u => u.MenuItemId == menuItemId);
                if(cartItemCart == null)
                {
                    //Stavka ne postoji u korpi
                    CartItem newCartItem = new()
                    {
                        MenuItemId = menuItemId,
                        Quantity = updateQuantityBy,
                        ShoppingCartId = shoppingCart.Id,
                        MenuItem = null,
                    };
                    _db.CartItems.Add(newCartItem);
                    _db.SaveChanges();
                }
                else
                {
                    //Stavka vec postoji u korpi, moramo azurirati samo kvantitet
                    int newQuantity = cartItemCart.Quantity + updateQuantityBy;
                    if(updateQuantityBy == 0 || newQuantity <=0)
                    {
                        //Brisem cartItem iz korpe
                        _db.CartItems.Remove(cartItemCart);
                        if(shoppingCart.CartItems.Count() == 1)
                        {
                            _db.ShoppingCarts.Remove(shoppingCart);
                        }
                        _db.SaveChanges();
                    }
                    else
                    {
                        cartItemCart.Quantity = newQuantity;
                        _db.SaveChanges();
                    }
                }
            }
            return _response;
        }
    }
}
