﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace LumiSnap.VM
{
    public class LumiSnapSettingsVM : INPC
    {
        private string collisionViewName;
        private string distanceRev;
        private string distanceFwd;

        public event EventHandler OnRequestClose;

        public ObservableCollection<CollisionCatItem> CollisionCatItems { get; set; }
        public ObservableCollection<string> CollisionLinkNames
        {
            get
            {
                ObservableCollection<string> R = new ObservableCollection<string>();
                foreach (var item in CollisionLinks)
                {
                    R.Add(item.Title);
                }
                return R;
            }
        }

        public List<Document> CollisionLinks;
        private int selectedIndex;

        public string CollisionViewName
        {
            get => collisionViewName; set
            {
                if (collisionViewName != value)
                {
                    collisionViewName = value;
                    MyPropertyChanged(nameof(CollisionViewName));
                }
            }
        }
        public string DistanceRev
        {
            get => distanceRev; set
            {
                if (distanceRev != value)
                {
                    distanceRev = value;
                    MyPropertyChanged(nameof(DistanceRev));
                }

            }
        }
        public string DistanceFwd
        {
            get => distanceFwd; set
            {
                if (distanceFwd != value)
                {
                    distanceFwd = value;
                    MyPropertyChanged(nameof(DistanceFwd));
                }

            }
        }

        public int SelectedIndex
        {
            get => selectedIndex; set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    MyPropertyChanged(nameof(SelectedIndex));
                }

            }
        }

        public Document SelectedDocument
        {
            get
            {
                return CollisionLinks[SelectedIndex];
            }
        }

        public Result RevitTransactionResult { get; internal set; }

        public LumiSnapSettingsVM()
        {
            CollisionCatItems = new ObservableCollection<CollisionCatItem>();
            CollisionLinks = new List<Document>();

        }
    }

    public class CollisionCatItem : INPC
    {
        private string name;
        private bool selected;

        public string Name
        {
            get => name; set
            {
                if (name != value)
                {
                    name = value;
                    MyPropertyChanged(nameof(Name));
                }

            }
        }

        public bool Selected
        {
            get => selected; set
            {
                if(selected != value)
                {
                    selected = value;
                    MyPropertyChanged(nameof(Selected));
                }
                
            }
        }

    }
     

}