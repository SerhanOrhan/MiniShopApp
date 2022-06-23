﻿using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniShopApp.Business.Abstract;
using MiniShopApp.Core;
using MiniShopApp.Entity;
using MiniShopApp.WebUI.Identity;
using MiniShopApp.WebUI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderItem = MiniShopApp.Entity.OrderItem;

namespace MiniShopApp.WebUI.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private ICardService _cardService;
        private UserManager<User> _userManager;
        private IOrderService _orderService;

        public CardController(ICardService cardService, UserManager<User> userManager, IOrderService orderService)
        {
            _cardService = cardService;
            _userManager = userManager;
            _orderService = orderService;
        }

        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);//autorize olunca olan userın ıdsını alır.
            var card = _cardService.GetCardByUserId(userId);
            return View(new CardModel()
            {
                CardId = card.Id,
                CardItems = card.CardItems.Select(i => new CardItemModel()
                {
                    CardItemId = i.Id,
                    ProductId = i.ProductId,
                    Name = i.Product.Name,
                    Price = (double)i.Product.Price,
                    ImageUrl = i.Product.ImageUrl,
                    Quantity = i.Quantity
                }).ToList()
            });
        }
        public IActionResult GetOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = _orderService.GetOrders(userId);
            var orderListModel = new List<OrderListModel>();
            OrderListModel orderModel;
            foreach (var item in orders)
            {
                orderModel = new OrderListModel();
                orderModel.OrderId = item.Id;
                orderModel.OrderNumber = item.OrderNumber;
                orderModel.OrderDate = item.OrderDate;
                orderModel.FirstName = item.FirstName;
                orderModel.LastName = item.LastName;
                orderModel.Address = item.Address;
                orderModel.City = item.City;
                orderModel.Phone = item.Phone;
                orderModel.Email = item.Email;
                orderModel.OrderState = item.OrderState;
                orderModel.PaymentType = item.PaymentType;
                orderModel.OrderItems = item.OrderItems.Select(i => new OrderItemModel()
                {
                    OrderItemId = i.Id,
                    Name=i.Product.Name,
                    Price=(double)i.Price,
                    Quantity=i.Quantity,
                    ImageUrl=i.Product.ImageUrl
                }).ToList();
                orderListModel.Add(orderModel);
            }
            return View("Orders",orderListModel);
        }
        [HttpPost]
        public IActionResult AddToCard(int productId, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            _cardService.AddToCard(userId, productId, quantity);
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteFromCard(int productId)
        {
            var userId = _userManager.GetUserId(User);
            _cardService.DeleteFromCard(userId, productId);
            return RedirectToAction("Index");
        }
        public IActionResult CheckOut()
        {
            var userId = _userManager.GetUserId(User);
            var card = _cardService.GetCardByUserId(userId);
            var orderModel = new OrderModel();
            orderModel.CardModel = new CardModel()
            {
                CardId = card.Id,
                CardItems = card.CardItems.Select(i => new CardItemModel()
                {
                    CardItemId = i.Id,
                    ProductId = i.ProductId,
                    Name = i.Product.Name,
                    Price = (double)i.Product.Price,
                    ImageUrl = i.Product.ImageUrl,
                    Quantity = i.Quantity,

                }).ToList()
            };
            return View(orderModel);
        }
        [HttpPost]
        public IActionResult CheckOut(OrderModel orderModel)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var card = _cardService.GetCardByUserId(userId);
                orderModel.CardModel = new CardModel()
                {
                    CardId = card.Id,
                    CardItems = card.CardItems.Select(i => new CardItemModel()
                    {
                        CardItemId = i.Id,
                        ProductId = i.ProductId,
                        Name = i.Product.Name,
                        Price = (double)i.Product.Price,
                        ImageUrl = i.Product.ImageUrl,
                        Quantity = i.Quantity
                    }).ToList()
                };
                if (!CardNumberControl(orderModel.CardNumber))
                {
                    TempData["Message"] = JobManager.CreateMessage("Hata", "Kart Numarası Hatalıdır", "danger");
                    _cardService.ClearCard(orderModel.CardModel.CardId);
                    return View(orderModel);
                }
                //Ödeme alma işlemlerine başlayacağız
                var payment = PaymentProcess(orderModel);
                if (payment.Status=="success")
                {
                    TempData["Message"] = JobManager.CreateMessage("BAŞARILI", "Ödemeniz başarıyla alınmıştır", "success");
                    _cardService.ClearCard(orderModel.CardModel.CardId);
                    return View("Success");
                }
                else
                {
                    TempData["Message"] = JobManager.CreateMessage("BAŞARISIZ",payment.ErrorMessage, "danger");
                }
            }
            return View(orderModel);
        }

        private bool CardNumberControl(string cardNumber)
        {
            var cardNumberLength = cardNumber.Length;
            int total=0;
            if (cardNumberLength!=16)
            {
                return false;
            }
            else
            {
                int ovenTotal = 0;
                int oddTotal = 0;
                for (int i = 0; i < cardNumberLength; i++)
                {
                    int nextNumber =int.Parse((cardNumber[i].ToString()));
                    if (i%2==0)
                    {
                        oddTotal += NumberControl((nextNumber * 2).ToString());
                    }
                    else
                    {
                        ovenTotal += nextNumber;
                    }
                }
                total = ovenTotal + oddTotal;
            }
            if (total%10==0)
            {
                return true;
            }
            return false;
        }
        private int NumberControl(string number)
        {
            int numberLegth = number.Length;
            if (numberLegth==1)
            {
                return Convert.ToInt32(number);
            }
            int total = 0;
            for (int i = 0; i < numberLegth; i++)
            {
                total += Convert.ToInt32(number[i].ToString());
            }
            return total;
        }
        private void SaveOrder(OrderModel orderModel, Payment payment, string userId)
        {
            var order = new Order();
            order.OrderNumber = new Random().Next(111111111, 999999999).ToString();
            order.OrderState = EnumOrderState.Completed;
            order.PaymentType = EnumPaymentType.CreditCard;
            order.PaymentId = payment.PaymentId;
            order.ConversationId = payment.ConversationId;
            order.OrderDate = new DateTime();
            order.FirstName = orderModel.FirstName;
            order.LastName = orderModel.LastName;
            order.Phone = orderModel.Phone;
            order.Email = orderModel.Email;
            order.UserId = userId;
            order.Address = orderModel.Address;
            order.City = orderModel.City;
            order.OrderItems = new List<OrderItem>();
            foreach (var item in orderModel.CardModel.CardItems)
            {
                var orderItem = new OrderItem()
                {
                    Price=item.Price,
                    Quantity=item.Quantity,
                    ProductId=item.ProductId,
                };
                order.OrderItems.Add(orderItem);
            }
            //Artık bu bilgileri kullanarak order kaydı yaratabılırız
            _orderService.Create(order);
        }

        private Payment PaymentProcess(OrderModel orderModel)
        {
            Options options = new Options();
            options.ApiKey = "sandbox-WNeTysts76e7tPEGfEl6pdikI4q73neG";
            options.SecretKey = "sandbox-sAhINCxSAQwY1YUiK5qs9FANl4gdfIjz";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = new Random().Next(111111111,999999999).ToString();
            request.Price = orderModel.CardModel.TotalPrice().ToString();
            request.PaidPrice = orderModel.CardModel.TotalPrice().ToString();//indiirm felan olrusa burda yazılır ona gore odeme alınır
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = "B67832";
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            PaymentCard paymentCard = new PaymentCard();
            paymentCard.CardHolderName = orderModel.CardName;
            paymentCard.CardNumber = orderModel.CardNumber;
            paymentCard.ExpireMonth = orderModel.ExprationMonth;
            paymentCard.ExpireYear = orderModel.ExprationYear;
            paymentCard.Cvc = orderModel.Cvc;
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;

            //paymentCard.CardNumber = "5528790000000008";
            //paymentCard.ExpireMonth = "12";
            //paymentCard.ExpireYear = "2030";
            //paymentCard.Cvc = "123";

            Buyer buyer = new Buyer();
            buyer.Id = "BY789";
            buyer.Name = orderModel.FirstName;
            buyer.Surname = orderModel.LastName;
            buyer.GsmNumber = orderModel.Phone;
            buyer.Email = orderModel.Email;
            buyer.IdentityNumber = "74300864791";
            buyer.LastLoginDate = "2015-10-05 12:43:35";
            buyer.RegistrationDate = "2013-04-21 15:12:09";
            buyer.RegistrationAddress = orderModel.Address;
            buyer.Ip = "85.34.78.112";
            buyer.City = orderModel.City;
            buyer.Country = "Turkey";
            buyer.ZipCode = "34732";
            request.Buyer = buyer;

            Address shippingAddress = new Address();
            shippingAddress.ContactName = "Jane Doe";
            shippingAddress.City = "Istanbul";
            shippingAddress.Country = "Turkey";
            shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            shippingAddress.ZipCode = "34742";
            request.ShippingAddress = shippingAddress;

            Address billingAddress = new Address();
            billingAddress.ContactName = "Jane Doe";
            billingAddress.City = "Istanbul";
            billingAddress.Country = "Turkey";
            billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            billingAddress.ZipCode = "34742";
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();

            BasketItem basketItem;
            foreach (var item in orderModel.CardModel.CardItems)
            {
              basketItem = new BasketItem();
              basketItem.Id = item.ProductId.ToString();
              basketItem.Name = item.Name;
              basketItem.Category1 = "General";
              basketItem.ItemType = BasketItemType.PHYSICAL.ToString();
              basketItem.Price = (item.Price*item.Quantity).ToString();
              basketItems.Add(basketItem);
            }
            request.BasketItems = basketItems;//içindekileri iyzicodaki basketıtemse atıyoruz.
            return Payment.Create(request, options);
        }
    }
}