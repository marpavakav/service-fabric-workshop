﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    public class BasketController : BaseController
    {
        private readonly HttpClient http;

        public BasketController(HttpClient http)
        {
            this.http = http;
        }

        public async Task<IActionResult> Index()
        {
            using (var response = await http.GetAsync(await GetServiceUriAsync("Basket", "BasketApi", $"/api/basket/{GetBasketId()}")))
            {
                var products = await DeserializeResponseAsync<IEnumerable<Product>>(response);
                return View(products);
            }
        }

        public async Task<IActionResult> Add(Guid id, string redirect)
        {
            Product p;
            using (var response = await http.GetAsync(await GetServiceUriAsync("Products", "ProductApi", $"/api/products/{id}")))
            {
                p = await DeserializeResponseAsync<Product>(response);
            }

            using (var content = GetJsonContent(p))
            using (var response = await http.PostAsync(await GetServiceUriAsync("Basket", "BasketApi", $"/api/basket/{GetBasketId()}/items"), content))
            {
                return !string.IsNullOrEmpty(redirect) ?
                    (IActionResult)Redirect(redirect) :
                    RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            using (var response = await http.DeleteAsync(await GetServiceUriAsync("Basket", "BasketApi", $"/api/basket/{GetBasketId()}/items/{id}")))
            {
                return RedirectToAction("Index");
            }
        }
    }
}