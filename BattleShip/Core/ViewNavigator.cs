
﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace WPF_App.Core
{
    public class ViewNavigator : ObservableObject
    {
        public ICommand NavigateBackCommand { get; set; } // команда навигации назад

        public ObservableCollection<ViewObject> Views { get; } // коллекция представлений
        public ICollectionView ViewsCollectionView { get; set; } // представление коллекции

        public ViewObject CurrentViewObject { get; set; } // текущее представление

        private object _currentView;
        public object CurrentView // текущий вид
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(); }
        }

        private Stack<ViewObject> _viewStack; // стек представлений


        public ViewNavigator()
        {
            Views = new ObservableCollection<ViewObject>(); // инициализация коллекции
            ViewsCollectionView = CollectionViewSource.GetDefaultView(Views); // представление по умолчанию

            _viewStack = new Stack<ViewObject>(); // инициализация стека

            NavigateBackCommand = new RelayCommand(
                action => NavigateBack(), // действие навигации назад
                condition => this._viewStack.Count > 1); // условие выполнения
        }


        public ViewObject AddView(Type view, string caption, bool selected = false, bool singleton = false, object visual = null, Predicate<object> canExecute = null, Predicate<object> isVisible = null)
        {
            var viewObject = new ViewObject(this, view, caption, selected, singleton, visual, canExecute, isVisible); // создаем объект представления
            viewObject.OnNavigate += Navigate; // подписываемся на навигацию

            this.Views.Add(viewObject); // добавляем в коллекцию

            return viewObject; // возвращаем объект
        }

        public void OrderByCaption()
        {
            ViewsCollectionView.SortDescriptions.Add(new SortDescription(nameof(ViewObject.Caption), ListSortDirection.Ascending)); // сортировка по заголовку
        }

        public void Navigate(ViewObject viewObject)
        {
            this.NavigateToView(viewObject); // навигация к представлению
        }
        public void NavigateBack()
        {
            if (this._viewStack.Count > 1) // проверка что есть куда возвращаться
            {
                this._viewStack.Pop(); // убираем текущее
                this.NavigateToView(this._viewStack.Pop()); // переходим к предыдущему
            }
        }

        private void NavigateToView(ViewObject viewObject)
        {
            var view = viewObject.GetView(); // получаем представление

            if (this.CurrentView != view && view != null) // проверка что представление изменилось
            {
                this.CurrentView = view; // устанавливаем текущее
                this.CurrentViewObject = viewObject; // устанавливаем объект

                if (_viewStack.Any() && _viewStack.Peek() == viewObject)
                {
                    return; // уже в стеке
                }

                this._viewStack.Push(viewObject); // добавляем в стек

                //Устанавливаем IsSelected в true для выбранного представления
                foreach (var viewItem in Views)
                {
                    viewItem.IsSelected = viewItem == viewObject; // выделяем текущее
                }
            }
        }
    }

    public class ViewObject : ObservableObject
    {
        public event Action<ViewObject> OnNavigate; // событие навигации

        private ViewNavigator _viewNavigator; // навигатор
        private bool _singleton; // одиночка
        private object _instance; // экземпляр
        private Predicate<object> _isVisiblePredicate; // предикат видимости

        public Type ViewType { get; } // тип представления
        public bool IsVisible => this._isVisiblePredicate?.Invoke(this) == true; // видимость
        public object Visual { get; set; } // визуальный элемент

        private bool _isSelected;
        public bool IsSelected // выбрано ли
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private string _caption;
        public string Caption // заголовок
        {
            get { return _caption; }
            set { _caption = value; OnPropertyChanged(); }
        }

        public ICommand NavigateCommand { get; set; } // команда навигации

        public ViewObject(ViewNavigator viewNavigator, Type view,
            string caption, bool selected = false, bool singleton = false, object visual = null,
            Predicate<object> canExecute = null, Predicate<object> isVisible = null)
        {
            this._viewNavigator = viewNavigator; // навигатор
            this.ViewType = view; // тип представления
            this.Caption = caption; // заголовок
            this._singleton = singleton; // одиночка
            this.Visual = visual; // визуальный элемент
            this._isVisiblePredicate = isVisible ?? new Predicate<object>(condition => true); // предикат видимости

            NavigateCommand = new RelayCommand(
                action => Navigate(), // действие навигации
                canExecute ?? new Predicate<object>(condition => true)); // условие выполнения
        }

        public void Navigate()
        {
            OnNavigate?.Invoke(this); // вызываем событие
        }

        public object GetView()
        {
            if (_singleton) // если одиночка
            {
                if (_instance == null) // если экземпляр не создан
                {
                    _instance = Activator.CreateInstance(ViewType); // создаем
                }
                return _instance; // возвращаем существующий
            }

            return Activator.CreateInstance(ViewType); // создаем новый экземпляр
        }
    }
}
