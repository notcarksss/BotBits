﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayerIOClient;

namespace BotBits
{
    public class ShopData
    {
        private readonly Dictionary<string, int> _itemCounts = new Dictionary<string, int>();

        public ShopData(IEnumerable<VaultItem> items)
        {
            foreach (var item in items)
            {
                int count;
                this._itemCounts.TryGetValue(item.ItemKey, out count);
                this._itemCounts[item.ItemKey] = count;
            }
        }

        public bool HasSmiley(Smiley smiley)
        {
            var pack = ItemServices.GetPackage(smiley);
            if (pack == null) return true;

            return this._itemCounts.ContainsKey(pack);
        }

        public bool HasBlock(int id, int count, bool isBuildersClub, bool isAdministrator)
        {
            var pack = ItemServices.GetPackageInternal(id);
            if (pack == null) return true;

            //if (pack.BlocksPerPack > 0)
            //{
            //    int owned;
            //    this._itemCounts.TryGetValue(pack.Package, out owned);
            //    return (isBuildersClub && pack.Package != "brickdiamond") || 
            //           pack.BlocksPerPack * owned > count;
            //}

            switch (pack.Package)
            {
                case "admin":
                    return isAdministrator;
                case "bc":
                    return isBuildersClub;
                case "brickdiamond":
                case "brickhwthrophy":
                case "brickmagic":
                case "brickmagic2":
                case "brickmagic3":
                case "brickmagic4":
                case "brickmagic5":
                    check:
                    return this._itemCounts.ContainsKey(pack.Package);
                default:
                    if (isBuildersClub) return true;
                    goto check;
            }
        }
    }
}
