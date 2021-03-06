﻿using HoTS_Service.Entity;

namespace HoTS_Service.Entity
{

    /// <summary>
    /// сущность героя, которая объединяет информацию всех базовых сущностей
    /// </summary>
    public class HeroData
    {
        Hero hero;
        HeroDetails details;
        HeroClusters clusters;
        HeroStatisticItemAvg stat;
        int id;

        public HeroData()
        {

        }

        public HeroData(Hero hero, HeroDetails details, HeroClusters clusters, HeroStatisticItemAvg stat, int id)
        {
            this.hero = hero;
            this.details = details;
            this.Clusters = clusters;
            this.stat = stat;
            this.Id = id;
        }

        public Hero Hero { get => hero; set => hero = value; }
        public HeroDetails Details { get => details; set => details = value; }
        public int Id { get => id; set => id = value; }
        public HeroStatisticItemAvg Statistic { get => stat; set => stat = value; }
        public HeroClusters Clusters { get => clusters; set => clusters = value; }
    }


}
