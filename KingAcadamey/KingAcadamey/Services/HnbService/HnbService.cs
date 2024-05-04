﻿using KingAcadamey.Configuration;
using KingAcadamey.Exceptions;
using KingAcadamey.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KingAcadamey.Services
{
    public class HnbService : IHnbService
    {
        private readonly IOptions<ConnectionApi> _connectionApi;
        private readonly IArithemticMeanService _arithemticMeanService;

        public HnbService(IOptions<ConnectionApi> connectionApi, IArithemticMeanService arithemticMeanService)
        {
            _connectionApi = connectionApi;
            _arithemticMeanService = arithemticMeanService;
        }

        public async Task<List<ViewResultModel>> GetHnbValues(string dateFrom, string dateTo)
        {
            string url = _connectionApi.Value.ConnectionString;

            url = url.Replace("dateFrom", dateFrom);
            url = url.Replace("dateTo", dateTo);

            var client = new HttpClient();
            var request = new List<ViewResultModel>();
            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var reposnseContext = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(reposnseContext))
                    {
                        var results = new List<SearchResultModel>();

                        results = JsonSerializer.Deserialize<List<SearchResultModel>>(reposnseContext);

                        if(results != null)
                        {
                            var currencies = results.Select(x=> new HnbCurrency 
                            { 
                                BrojTecajnice = x.broj_tecajnice,
                                DatumPrimjene = DateTime.Parse(x.datum_primjene),
                                Drzava = x.drzava,
                                DrzavaIso = x.drzava_iso,
                                KupovniTecaj = Decimal.Parse(x.kupovni_tecaj),
                                ProdajniTecaj = Decimal.Parse(x.prodajni_tecaj),
                                SifraValute = x.sifra_valute,
                                SrednjiTecaj = Decimal.Parse(x.srednji_tecaj),
                                valuta = x.valuta
                            
                            }).ToList();   

                            if(currencies != null)
                            {
                                var TecajKupovni = currencies.Select(x => x.KupovniTecaj).ToList();
                                var prosjekKupovniTecaj = _arithemticMeanService.CalculateArithmeticMean(TecajKupovni);
                                var TecajProdajni = currencies.Select(x=> x.ProdajniTecaj).ToList();
                                var prosjekProdajniTecaj = _arithemticMeanService.CalculateArithmeticMean(TecajProdajni);
                                var TecajSrednji = currencies.Select(x => x.SrednjiTecaj).ToList();
                                var prosjekSrednjiTecaj = _arithemticMeanService.CalculateArithmeticMean(TecajSrednji);
                                var model = new ViewResultModel();
                                model.ProsjekKupovniTecaj = prosjekKupovniTecaj;
                                model.ProsjekSrednjiTecaj = prosjekSrednjiTecaj;
                                model.ProsjekProdajniTecaj = prosjekProdajniTecaj;
                                request.Add(model);
                            }
                        }
                        else
                        {
                            throw new ErrorMessage("No results to match this parameters");
                        }
                   
                    }
                    else
                    {
                        throw new ErrorMessage("No response!");
                    }

                }
            }
            catch(Exception ex)
            {
                throw new ErrorMessage($"An error occurred: {ex.Message}");
            }

            return request;
        }
    }
}