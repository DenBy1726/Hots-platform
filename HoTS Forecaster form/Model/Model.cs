﻿using HoTS_Service.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HoTS_Service.Entity;
using HoTS_Service.Entity.Enum;

namespace HoTS_Forecaster_form
{
    public class Model
    {
        private HeroService hero = new HeroService();

        /// <summary>
        /// сервис для работы с картами
        /// </summary>
        public MapService Map = new MapService();

        private HeroDetailsService detail = new HeroDetailsService();

        private HeroClustersSevice clusters = new HeroClustersSevice();

        /// <summary>
        /// сервис для работы со статистикой
        /// </summary>
        public StatisticModule Statistic = new StatisticModule();



        public List<NeuralNetworkForecast> ForecastService = new List<NeuralNetworkForecast>();

        /// <summary>
        /// данные о героях
        /// </summary>
        public HeroList Hero { get => new HeroList(
            hero.All().ToList(),
            detail.All().ToList(),
            clusters.All().ToList(),
            Statistic.HeroStatistic.All().Item1.ToList());}

        public Model()
        {
            Validate("./Data/Hero.json");
            Validate("./Data/HeroDetails.json");
            Validate("./Data/Map.json");
            Validate("./Data/HeroClusters.json");

            ForecastService = Directory.GetFiles("./Data/AI/")
                .Select(name => new NeuralNetworkForecast().Load(name))
                .ToList();
            try
            {
                hero.Load("./Data/Hero.json");
            }
            catch(Exception e)
            {
                ExceptionThrower.Throw(405,e, "Hero.json");
            }
            try
            {
                Map.Load("./Data/Map.json");
            }
            catch (Exception e)
            {
                ExceptionThrower.Throw(405,e, "Map.json");
            }
            try
            {
                detail.Load("./Data/HeroDetails.json");
            }
            catch (Exception e)
            {
                ExceptionThrower.Throw(405,e, "HeroDetails.json");
            }
            try
            {
                clusters.Load("./Data/HeroClusters.json");
            }
            catch (Exception e)
            {
                ExceptionThrower.Throw(405, e, "HeroClusters.json");
            }

        }

        /// <summary>
        /// Проверка пути файла на корректность
        /// </summary>
        /// <param name="file"></param>
        private void Validate(string file)
        {
            //если файла нету, кинуть исключение
            if (File.Exists(file) == false)
                ExceptionThrower.Throw(404,null,file);
        }
 
    }
    
}
