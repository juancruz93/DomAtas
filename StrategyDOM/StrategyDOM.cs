// Copyright QUANTOWER LLC. © 2017-2020. All rights reserved.

using System;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;
using TLSharp;
using TLSharp.Core;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using TeleSharp.TL.Messages;
using TLSharp.Core.Exceptions;
using TLSharp.Core.Network;
using TLSharp.Core.Network.Exceptions;
using TLSharp.Core.Utils;
using TradingPlatform.BusinessLayer.Integration;
using System.Timers;

namespace StrategyDOM2
{




}

    namespace StrategyDOM
{

    /// <summary>
    /// An example of blank strategy. Add your code, compile it and run via Strategy Runner panel in the assigned trading terminal.
    /// Information about API you can find here: http://api.quantower.com
    /// </summary>
    public class StrategyDOM : Strategy
    {

        [InputParameter("Symbol", 0)]
        public Symbol symbol;

        [InputParameter("Account", 1)]
        public Account account;

        //verifica posicion abierta.
        public int verificoordenactiva = 0;

        // mensaje para enviar
        public string moneda;
        public string mimensaje = "mensaje";
        public string precio;

        public int orden = 0;

        public int activado = 0;

        public int logprueba = 0;
        public int pruebaorden = 0;

        // Valores definidos para operar
        public double valorordenlimitada = 200;

        // Valores que representan los precios manejados
        public string TPpriceentry = "0,010";
        public string SLpriceentry = "0,005";

        public string entrybuyprice = "0,001";
        public string entrysellprice = "0,001";

        public string levelask2price = "0,001";
        public string levelask3price = "0,002";
        public string levelask4price = "0,005";

        public string levelbid2price = "0,001";
        public string levelbid3price = "0,002";
        public string levelbid4price = "0,003";

        // Cantidad de lotes
        public string QuantityL = "0,01";

        // Precio bestask y bestbib
        public double precioactualask = 0;
        public double precioactualbid = 0;

        // Orden limit ASK
        public double limitaskprice = 0;
        public double limitaskvolume = 0;
        public DateTime limitasktime;


        // Orden limit BID
        public double limitbidprice = 0;
        public double limitbidvolume = 0;
        public DateTime limitbidtime;

        // espera de 10 segundos
        public int oneorden = 0;
        public string onedirection;
        public double priceasktime;
        public double pricebidtime;
        public double lognombre;

        public int ordenactivamanual = 0;

        // gestiona los segundos 10 en operacion Ask
        public List<DateTime> limitsask = new List<DateTime>();
        // gestiona los segundos 10 en operacion Bid
        public List<DateTime> limitsbid = new List<DateTime>();

        // gestiona volumen entre los 10 segundos Ask
        public List<Double> limitsaskvolumen = new List<Double>();
        // gestiona volumen entre los 10 segundos Bid
        public List<Double> limitsbidvolumen = new List<Double>();

        // gestiona 5 segundos para cerrar operacion Ask
        public List<DateTime> limitsaski = new List<DateTime>();
        // gestiona 5 segundos para cerrar operacion Bid
        public List<DateTime> limitsbidi = new List<DateTime>();

        // valiable entrando de TP y SL
        public int entraTPSL = 0;
        public double entrypricesignal = 0;
        public double TPsignal = 0;
        public double SLsignal = 0;
        public string directionsignal;

        public int oneordenactivate = 0;

        //orden activa
        public double ordenpricelimit;
        public double ordenpriceentry;
        public double ordenvolume;
        public DateTime ordentime;
        public bool ordenactiva = false;
        public string ordendirection;

        // controla ordenes 
        public bool entrytrade = false;

        // un valor que valida si entra a la posicion con Ask o Bid
        public int priceentryask;
        public int priceentrybid;

        /// <summary>
        /// Strategy's constructor. Contains general information: name, description etc. 
        /// </summary>
        public StrategyDOM()
            : base()
        {
            // Defines strategy's name and description.
            this.Name = "StrategyDOM";
            this.Description = "My strategy's annotation";

        }

 


        protected override void OnRun()
        {
            try
            {
               // Get first available symbol
               // Symbol symbol = Core.Instance.Symbols.FirstOrDefault();
               this.symbol = Core.Instance.Symbols.FirstOrDefault(s => s.Name == "ATOM/USDT");

                if (this.symbol == null)
                    return;

                this.symbol.NewLevel2 += this.SymbolOnNewLevel2;

                var timer = new System.Timers.Timer(TimeSpan.FromMilliseconds(120000).TotalMilliseconds); // se ejecutara cada 5 min
                timer.Elapsed += async (sender, e) =>
                {
                    Log("Continua Activo");
                    //calcularentrada();
                };

                timer.Start(); // indicamos que unicie               
               
            }
            catch (Exception ex)
            {
                Log("error:" + ex);
            }
        }

        protected override void OnStop()
        {
            if (this.symbol != null)
                return;

            this.symbol.NewLevel2 -= this.SymbolOnNewLevel2;         
        }



        public void calcularentrada()
        {


            // verifica orden ya activa 
            if (verificoordenactiva == 0)
            {
                foreach (Position pos in Core.Positions)
                {

                    Log($"PositionID name: {pos.State} ");

                    verificoordenactiva = 1;
                }

            }


            // chequea la orden activa y si hay ordenes limitadas menores al valor establecido superando los 4 segundos quita la orden 
            if (verificoordenactiva == 1)
            {
                if (oneorden == 2 && ordenactiva == true)
                {
                    // BUY
                    if (ordendirection == "BUY")
                    {
                        // trae las ordenes limitadas que entran a mercado
                        var price = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();
                        var priceresultalimitask = (from s in price.Asks
                                                    orderby s.Price ascending
                                                    where s.Price == ordenpricelimit
                                                    select s).First();

                        if (priceresultalimitask != null)
                        {
                            if (priceresultalimitask.Size < valorordenlimitada)
                            {
                                limitsaski.Add(priceresultalimitask.QuoteTime);
                                for (int i = 0; i < limitsaski.Count; i++)
                                {
                                    TimeSpan span = (limitsaski[0] - priceresultalimitask.QuoteTime);

                                    // formatos para visualizar dias, horas, minutos, segundos sobre diferencia de orden
                                    String.Format("{0} days, {1} hours, {2} minutes, {3} seconds",
                                        span.Days, span.Hours, span.Minutes, span.Seconds);

                                    int segund = Convert.ToInt32(System.Math.Abs(span.Seconds));
                                    //this.LogWarn("diferencia segundos= " + segund.ToString());

                                    // Si hay ordenes pequeñas entrantes mayores a 4 segundos cancela la operacion pendiente.
                                    if (segund > 4)
                                    {
                                        // Cancela orden y limpia todo
                                        CancelActiveOrden();
                                        Cleanervariables();
                                        verificoordenactiva = 0;
                                        break;
                                    }

                                }
                            }

                        }
                    }

                    // SELL
                    if (ordendirection == "SELL")
                    {
                        // trae las ordenes limitadas que entran a mercado
                        var price = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();
                        var priceresultalimitbid = (from s in price.Bids
                                                    orderby s.Price ascending
                                                    where s.Price == ordenpricelimit
                                                    select s).First();


                        if (priceresultalimitbid != null)
                        {
                            if (priceresultalimitbid.Size < valorordenlimitada)
                            {
                                limitsbidi.Add(priceresultalimitbid.QuoteTime);
                                for (int i = 0; i < limitsbidi.Count; i++)
                                {
                                    TimeSpan span = (limitsbidi[0] - priceresultalimitbid.QuoteTime);

                                    // formatos para visualizar dias, horas, minutos, segundos sobre diferencia de orden
                                    String.Format("{0} days, {1} hours, {2} minutes, {3} seconds",
                                        span.Days, span.Hours, span.Minutes, span.Seconds);

                                    int segund = Convert.ToInt32(System.Math.Abs(span.Seconds));
                                    //this.LogWarn("diferencia segundos= " + segund.ToString());

                                    // Si hay ordenes pequeñas entrantes mayores a 4 segundos cancela la operacion pendiente.
                                    if (segund > 4)
                                    {
                                        // Cancela orden y limpia todo
                                        CancelActiveOrden();
                                        Cleanervariables();
                                        verificoordenactiva = 0;
                                        break;
                                    }

                                }
                            }

                        }
                    }
                }

            }






            
        }
        private void SymbolOnNewLevel2(Symbol symbol, Level2Quote level2, DOMQuote dom)
        {
            if (activado== 0)
            {
                Log("Activado");
                activado = 1;
            }


            
                var priceask = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();

                var pricebid = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();

                var resultask = (from s in priceask.Asks
                                 orderby s.Price ascending
                                 select s).First();

                var resultbid = (from s in pricebid.Bids
                                 orderby s.Price descending
                                 select s).First();



            double levelask2 = resultbid.Price + Convert.ToDouble(levelask4price);
            symbol = Core.Instance.Symbols.FirstOrDefault(s => s.Name == "ATOM/USDT");
            if (pruebaorden == 0)
            {

                var request = new PlaceOrderRequestParameters
                {
                    Symbol = symbol,                        // Mandatory
                    Account = account,                      // Mandatory
                    Side = Side.Buy,                        // Mandatory
                    OrderTypeId = "Stop",         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                    Quantity = Convert.ToDouble(QuantityL),                         // Mandatory

                    //Price = levelask2,                           // Optional. Required for limit and stop limit orders
                    TriggerPrice = levelask2,                    // Optional. Required for stop and stop limit orders
                                                                 //TrailOffset = 20,                       // Optional. Required for trailing stop order type

                   // TimeInForce = TimeInForce.GTC,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                                                            //ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                                                            //StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                                                            //TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
                };

                // Send request
                var result = Core.Instance.PlaceOrder(request);
                result = Core.Instance.PlaceOrder(symbol, account, Side.Buy, triggerPrice: levelask2);
                Log("Envia orden de compran entry:" + levelask2);


                pruebaorden = 1;
            }



            if (resultask.Size >= valorordenlimitada)
                {
                 
                    //Log("nivel 1: ASK" + resultask.Price + ", " + resultask.Size);
                    limitaskprice = resultask.Price;
                    limitaskvolume = resultask.Size;
                    limitasktime = resultask.QuoteTime;
                    onedirection = "BUY";
                    if (oneordenactivate == 0)
                    {
                        oneorden = 1;
                    }
                }



                if (resultbid.Size >= valorordenlimitada)
                {
                    //Log("nivel 1: BID" + resultbid.Price + ", " + resultbid.Size);
                    limitbidprice = resultbid.Price;
                    limitbidvolume = resultbid.Size;
                    limitbidtime = resultbid.QuoteTime;
                    onedirection = "SELL";
                    if (oneordenactivate == 0)
                    {
                        oneorden = 1;
                    }
                }

                precioactualask = resultask.Size;
                precioactualbid = resultbid.Size;

            
            //*****************************************************************
            // primera direccion para 10 segundos
            if (oneorden == 1)
            {
                // BUY            
                if (onedirection == "BUY")
                {
                    // trae las ordenes limitadas que entran a mercado
                    var price = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();

                    var priceresultalimitask = (from s in price.Asks
                                                orderby s.Price ascending
                                                where s.Price == limitaskprice
                                                select s).First();

                    //Log("Buy10: " + priceresultalimitask.Size + ", tamalo " + priceresultalimitask.Price);


                    // si el precio entrante es diferente a la guardada anteriormente restablece y limpia ordenes limitadas guardadas
                    if (priceasktime != limitaskprice)
                    {
                        limitsask.Clear();
                        limitsaskvolumen.Clear();
                        onedirection = "Sin direccion";
                    }

                    //guarda el precio actual del limit
                    priceasktime = limitaskprice;


                    // busca la orden correspondiente con la entrante
                    if (priceresultalimitask.Price == limitaskprice)
                    {
                        // guarda orden limitada entrante
                        limitsask.Add(limitasktime);
                        limitsaskvolumen.Add(priceresultalimitask.Size);
                        var sendorden = true;
                        for (int i = 0; i < limitsask.Count; i++)
                        {

                            // selecciona la primer orden limitada entrante y le resta el tiempo de la ultima entrante
                            TimeSpan span = (limitsask[0] - limitasktime);

                            // formatos para visualizar dias, horas, minutos, segundos sobre diferencia de orden
                            String.Format("{0} days, {1} hours, {2} minutes, {3} seconds",
                                span.Days, span.Hours, span.Minutes, span.Seconds);

                            int segund = Convert.ToInt32(System.Math.Abs(span.Seconds));
                            //this.LogWarn("diferencia segundos= " + segund.ToString());

                            // si la orden supera los 10 valida la operacion entrante
                            if (segund > 10)
                            {
                                
                                for (int a = 0; a < limitsaskvolumen.Count; a++)
                                {

                                    if (limitsaskvolumen[a] < valorordenlimitada)
                                    {
                                        Log("Novalida BUY volumen mal");
                                        sendorden = false;
                                        break;
                                    }
                                }

                                if (sendorden == true)
                                {
                                    oneorden = 2;
                                    oneordenactivate = 1;
                                    break;
                                }

                                if (sendorden == false)
                                {
                                    Cleanervariables();
                                    break;
                                }

                            }


                        }


                        // ENTRA ORDEN DE COMPRA BUY
                        if (oneorden == 2)
                        {
                            Log("entrydomB");
                            entrytradeDOM(limitaskprice, limitaskvolume, limitasktime, "BUY");
                        }


                    }
                }
                //***************************************************************************
                // SELL
                if (onedirection == "SELL")
                {

                    // trae las ordenes limitadas que entran a mercado
                    var price = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();

                    var priceresultalimitbid = (from s in price.Bids
                                                orderby s.Price ascending
                                                where s.Price == limitbidprice
                                                select s).First();


                    //Log("SELL10: " + priceresultalimitbid.Size + ", tamalo " + priceresultalimitbid.Price);

                    // si el precio entrante es diferente a la guardada anteriormente restablece y limpia ordenes limitadas guardadas
                    if (pricebidtime != limitbidprice)
                    {
                        limitsbid.Clear();
                        limitsbidvolumen.Clear();
                        onedirection = "Sin direccion";
                    }

                    //guarda el precio actual del limit
                    pricebidtime = limitbidprice;

                    // busca la orden correspondiente con la entrante
                    if (priceresultalimitbid.Price == limitbidprice)
                    {
                        // guarda orden limitada entrante
                        limitsbid.Add(limitbidtime);
                        limitsbidvolumen.Add(priceresultalimitbid.Size);
                        var sendorden = true;
                        for (int i = 0; i < limitsbid.Count; i++)
                        {

                            // selecciona la primer orden limitada entrante y le resta el tiempo de la ultima entrante
                            TimeSpan span = (limitsbid[0] - limitbidtime);

                            // formatos para visualizar dias, horas, minutos, segundos sobre diferencia de orden
                            String.Format("{0} days, {1} hours, {2} minutes, {3} seconds",
                                span.Days, span.Hours, span.Minutes, span.Seconds);

                            int segund = Convert.ToInt32(System.Math.Abs(span.Seconds));
                            //this.LogWarn("diferencia segundos= " + segund.ToString());

                            // si la orden supera los 10 valida la operacion entrante
                            if (segund > 10)
                            {
                                //Log("diferencia segundos= " + segund.ToString());
                                for (int a = 0; a < limitsbidvolumen.Count; a++)
                                {

                                    if (limitsbidvolumen[a] < valorordenlimitada)
                                    {
                                        Log("Novalida SELL volumen mal");
                                        sendorden = false;
                                        break;
                                    }
                                }

                                if (sendorden == true)
                                {
                                    oneorden = 2;
                                    oneordenactivate = 1;
                                    break;
                                }
                                if (sendorden == false)
                                {
                                    Cleanervariables();
                                    break;
                                }

                            }
                        }


                        // ENTRA ORDEN DE VENTA SELL
                        if (oneorden == 2)
                        {
                            Log("entrydomS");
                            entrytradeDOM(limitbidprice, limitbidvolume, limitbidtime, "SELL");
                        }

                    }
                }
            }

        

        }


        public void CheckActiveOrdersManual()
        {
            
            // verifica orden ya activa 
            if (ordenactivamanual == 0) { 

                foreach (Order pos in Core.Orders)
                {
                    lognombre = pos.TriggerPrice;
                    ordenactivamanual = 1;
                }
            }

            if (ordenactivamanual == 1){

                Log($"PositionID name: {lognombre} ");
                ordenactivamanual = 3;
            }


            if (ordenactivamanual == 2)
            {


                if (ordendirection == "BUY")
                {



                    //BUY
                    double TPbuy = ordenpriceentry + Convert.ToDouble(TPpriceentry);
                    double SLbuy = ordenpriceentry - Convert.ToDouble(SLpriceentry);


                    OpenPositionTPandSL("BUY", TPbuy, SLbuy);
                    entraTPSL = 1;
                    entrypricesignal = ordenpriceentry;
                    TPsignal = TPbuy;
                    SLsignal = SLbuy;
                    directionsignal = "BUY";
                    Cleanervariables();



                }
                if (ordendirection == "SELL")
                {

                    //SELL
                    double TPsell = ordenpriceentry - Convert.ToDouble(TPpriceentry);
                    double SLsell = ordenpriceentry + Convert.ToDouble(SLpriceentry);

                    OpenPositionTPandSL("SELL", TPsell, SLsell);
                    entraTPSL = 1;
                    entrypricesignal = ordenpriceentry;
                    TPsignal = TPsell;
                    SLsignal = SLsell;
                    directionsignal = "SELL";
                    Cleanervariables();


                }


            }


        }

        // chequea orden que se encuentra activa para poner TP y SL
        public void CheckActiveOrders(double askprice, double bidprice)
        {
            if (ordenactiva == true && entraTPSL == 0)
            {
                if (ordendirection == "BUY")
                {
                    // verifica que el ASK alla tocado el precio de entrada
                    if (askprice > ordenpriceentry && ordenpriceentry != 0)
                    {
                        priceentryask = 1;
                    }

                    if (priceentryask == 1)
                    {
                        //BUY
                        double TPbuy = ordenpriceentry + Convert.ToDouble(TPpriceentry);
                        double SLbuy = ordenpriceentry - Convert.ToDouble(SLpriceentry);


                        OpenPositionTPandSL("BUY", TPbuy, SLbuy);
                        entraTPSL = 1;
                        entrypricesignal = ordenpriceentry;
                        TPsignal = TPbuy;
                        SLsignal = SLbuy;
                        directionsignal = "BUY";
                        Cleanervariables();

                    }

                }
                if (ordendirection == "SELL")
                {
                    // verifica que el BID alla tocado el precio de entrada
                    if (bidprice < ordenpriceentry && ordenpriceentry != 0)
                    {
                        priceentrybid = 1;
                    }

                    if (priceentrybid == 1)
                    {
                        //SELL
                        double TPsell = ordenpriceentry - Convert.ToDouble(TPpriceentry);
                        double SLsell = ordenpriceentry + Convert.ToDouble(SLpriceentry);

                        OpenPositionTPandSL("SELL", TPsell, SLsell);
                        entraTPSL = 1;
                        entrypricesignal = ordenpriceentry;
                        TPsignal = TPsell;
                        SLsignal = SLsell;
                        directionsignal = "SELL";
                        Cleanervariables();
                       
                    }
                }
            }

        }




        public void OpenPositionTPandSL(string orden, double TP, double SL)
        {

            if (orden == "BUY")
            {
                // Orden TP
                var requestTP = new PlaceOrderRequestParameters
                {
                    Symbol = symbol,                        // Mandatory
                    Account = account,                      // Mandatory
                    Side = Side.Sell,                        // Mandatory
                    OrderTypeId = OrderType.Limit,         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                    Quantity = Convert.ToDouble(QuantityL),                         // Mandatory

                    Price = TP,                           // Optional. Required for limit and stop limit orders
                    //TriggerPrice = TP,                    // Optional. Required for stop and stop limit orders
                    //TrailOffset = 20,                       // Optional. Required for trailing stop order type

                    //TimeInForce = TimeInForce.Day,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                    //ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                    //StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                    //TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
                };

                // Send request
                var resultTP = Core.Instance.PlaceOrder(requestTP);

                // Orden SL
                var requestSL = new PlaceOrderRequestParameters
                {
                    Symbol = symbol,                        // Mandatory
                    Account = account,                      // Mandatory
                    Side = Side.Sell,                        // Mandatory
                    OrderTypeId = OrderType.Stop,         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                    Quantity = Convert.ToDouble(QuantityL),                         // Mandatory

                    //Price = SL,                           // Optional. Required for limit and stop limit orders
                    TriggerPrice = SL,                    // Optional. Required for stop and stop limit orders
                    //TrailOffset = 20,                       // Optional. Required for trailing stop order type

                    //TimeInForce = TimeInForce.Day,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                    //ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                    //StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                    //TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
                };

                // Send request
                var resultSL = Core.Instance.PlaceOrder(requestSL);

                Log("TP Orden= " + TP);
                Log("SL Orden= " + SL);
            }
            if (orden == "SELL")
            {
                // Orden TP
                var requestTP = new PlaceOrderRequestParameters
                {
                    Symbol = symbol,                        // Mandatory
                    Account = account,                      // Mandatory
                    Side = Side.Buy,                        // Mandatory
                    OrderTypeId = OrderType.Limit,         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                    Quantity = Convert.ToDouble(QuantityL),                         // Mandatory

                    Price = TP,                           // Optional. Required for limit and stop limit orders
                    //TriggerPrice = TP,                    // Optional. Required for stop and stop limit orders
                    //TrailOffset = 20,                       // Optional. Required for trailing stop order type

                    //TimeInForce = TimeInForce.Day,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                    //ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                    //StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                    //TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
                };

                // Send request
                var resultTP = Core.Instance.PlaceOrder(requestTP);

                // Orden SL
                var requestSL = new PlaceOrderRequestParameters
                {
                    Symbol = symbol,                        // Mandatory
                    Account = account,                      // Mandatory
                    Side = Side.Buy,                        // Mandatory
                    OrderTypeId = OrderType.Stop,         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                    Quantity = Convert.ToDouble(QuantityL),                         // Mandatory

                    //Price = SL,                           // Optional. Required for limit and stop limit orders
                    TriggerPrice = SL,                    // Optional. Required for stop and stop limit orders
                    //TrailOffset = 20,                       // Optional. Required for trailing stop order type

                    //TimeInForce = TimeInForce.Day,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                    //ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                    //StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                    //TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
                };

                // Send request
                var resultSL = Core.Instance.PlaceOrder(requestSL);

                Log("TP Orden= " + TP);
                Log("SL Orden= " + SL);
            }

        }

        public void entrytradeDOM(double limit, double volumen, DateTime timeask, string direction)
        {

            if (entrytrade == false)
            {
                // BUY
                if (direction == "BUY")
                {
                    if (limit != 0 && volumen != 0)
                    {

                        double entryprice = limit + Convert.ToDouble(entrybuyprice);
                        OpenPosition("BUY", entryprice);
                        entrytrade = true;
                        ordenpricelimit = limit;
                        ordenpriceentry = entryprice; //entra con mas + 0,001
                        ordenvolume = volumen;
                        ordentime = timeask;
                        ordenactiva = true;
                        ordendirection = "BUY";

                    }
                }
                // SELL
                if (direction == "SELL")
                {
                    if (limit != 0 && volumen != 0)
                    {

                        double entryprice = limit - Convert.ToDouble(entrysellprice);
                        OpenPosition("SELL", entryprice);
                        entrytrade = true;
                        ordenpricelimit = limit;
                        ordenpriceentry = entryprice; //entra con mas - 0,001
                        ordenvolume = volumen;
                        ordentime = timeask;
                        ordenactiva = true;
                        ordendirection = "SELL";

                    }
                }
            }
        }


        // chequea que cierre parciales
        public void CheckTPSLexit(double askprice, double bidprice)
        {
            if (entraTPSL == 1)
            {
                if (directionsignal == "BUY")
                {
                    if (bidprice >= TPsignal)
                    {
                        CancelActiveOrden();
                        Clearinfotpandsl();
                    }
                    if (bidprice <= SLsignal)
                    {
                        CancelActiveOrden();
                        Clearinfotpandsl();
                    }
                    if (directionsignal == "SELL")
                    {
                        if (askprice <= TPsignal)
                        {
                            CancelActiveOrden();
                            Clearinfotpandsl();
                        }
                        if (askprice >= SLsignal)
                        {
                            CancelActiveOrden();
                            Clearinfotpandsl();
                        }

                    }
                }
            }
        }


        // Ejecuta posicion de orden pendiente de compra o venta
        public void OpenPosition(string orden, double priceentry)
        {
            symbol = Core.Instance.Symbols.FirstOrDefault(s => s.Name == "ATOM/USDT");
            if (orden == "BUY")
            {
                // Orden de Compra
                
                var request = new PlaceOrderRequestParameters
                {
                    Symbol = symbol,                        // Mandatory
                    Account = account,                      // Mandatory
                    Side = Side.Buy,                        // Mandatory
                    OrderTypeId = OrderType.Stop,         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                    Quantity = Convert.ToDouble(QuantityL),                         // Mandatory

                    //Price = priceentry,                           // Optional. Required for limit and stop limit orders
                    TriggerPrice = priceentry,                    // Optional. Required for stop and stop limit orders
                    //TrailOffset = 20,                       // Optional. Required for trailing stop order type

                    TimeInForce = TimeInForce.GTC,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                    //ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                    //StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                    //TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
                };

                // Send request
                var result = Core.Instance.PlaceOrder(request);
                Log("Envia orden de compran entry:" + priceentry);

            }
            if (orden == "SELL")
            {
                // Orden de Venta
                
                var request = new PlaceOrderRequestParameters
                {
                    Symbol = symbol,                        // Mandatory
                    Account = account,                      // Mandatory
                    Side = Side.Sell,                        // Mandatory
                    OrderTypeId = OrderType.Stop,         // Mandatory. Variants: Market, Limit, Stop, StopLimit, TrailingStop
                    Quantity = Convert.ToDouble(QuantityL),                         // Mandatory

                    //Price = priceentry,                           // Optional. Required for limit and stop limit orders
                    TriggerPrice = priceentry,                    // Optional. Required for stop and stop limit orders
                    //TrailOffset = 20,                       // Optional. Required for trailing stop order type

                    //TimeInForce = TimeInForce.Day,          // Optional. Variants: Day, GTC, GTD, GTT, FOK, IOC
                    //ExpirationTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddDays(1), // Optional
                    //StopLoss = SlTpHolder.CreateSL(1.4),    // Optional
                    //TakeProfit = SlTpHolder.CreateTP(2.2)   // Optional
                };

                // Send request
                var result = Core.Instance.PlaceOrder(request);
                Log("Envia orden de Venta entry:" + priceentry);
            }

        }

        // cancela las operaciones pendientes
        public void CancelActiveOrden()
        {
            try
            {
                foreach (Order pos in Core.Orders)
                {
                    Core.Instance.CancelOrder(pos);
                    Log("Cierra orden enviada"); 
                }
               

            }
            catch (Exception e)
            {
               Log("error cancelar orden" + e);

                throw;
            }
        }


        // limpia toda info de trade
        public void Cleanervariables()
        {
            entrytrade = false;
            ordenpricelimit = 0;
            ordenpriceentry = 0;
            ordenvolume = 0;
            ordenactiva = false;
            ordendirection = "";
            oneorden = 0;
            oneordenactivate = 0;
            onedirection = "";
            limitsask.Clear();
            limitsbid.Clear();
            priceasktime = 0;
            pricebidtime = 0;
            limitsaski.Clear();
            limitsbidi.Clear();
            limitsaskvolumen.Clear();
            limitsbidvolumen.Clear();
            priceentryask = 0;
            priceentrybid = 0;
        }

        // limpia info de SL y TP
        public void Clearinfotpandsl()
        {
            entraTPSL = 0;
            entrypricesignal = 0;
            TPsignal = 0;
            SLsignal = 0;
            directionsignal = "";
        }


        public virtual async Task mensaje()
        {

            try
            {

                var client = new TelegramClient(2079632, "4fa72a41bea6699e46e71c5b6cd40b8b");
                await client.ConnectAsync();


                //var hash = await client.SendCodeRequestAsync("+573225536790");

                //var code = "93589"; // you can change code in debugger

                //var user = await client.MakeAuthAsync("+573225536790", hash, code);

                var result = await client.GetContactsAsync();

                //find recipient in contacts
                var user2 = result.Users
                    .Where(x => x.GetType() == typeof(TLUser))
                    .Cast<TLUser>()
                    .FirstOrDefault(x => x.Username == "leotrader01");

                if (user2 == null)
                {
                    throw new System.Exception("Usuario no est en la lista de contactos: ");
                }

                await client.SendTypingAsync(new TLInputPeerUser() { UserId = user2.Id });
                Thread.Sleep(3000);
                await client.SendMessageAsync(new TLInputPeerUser() { UserId = user2.Id }, mimensaje);
            }
            catch (Exception ex)
            {
                Log("error:" + ex);
            }
      

        }


        //public Level2Item[] Asks { get; }

        //public Level2Item[] Bids { get; }
        /// <summary>
        /// This function will be called after running a strategy
        ///// </summary>
        //protected override void OnRun()
        //{


        //}
        ///// <summary>
        ///// This function will be called after stopping a strategy
        ///// </summary>


        ///// <summary>
        ///// This function will be called after removing a strategy
        ///// </summary>
        //protected override void OnRemove()
        //{
        //    // Add your code here
        //}

        ///// <summary>
        ///// Use this method to provide run time information about your strategy. You will see it in StrategyRunner panel in trading terminal
        ///// </summary>
        //protected override List<StrategyMetric> OnGetMetrics()
        //{
        //    List<StrategyMetric> result = base.OnGetMetrics();

        //    // An example of adding custom strategy metrics:
        //    // result.Add("Opened buy orders", "2");
        //    // result.Add("Opened sell orders", "7");

        //    return result;
        //}
    }

}