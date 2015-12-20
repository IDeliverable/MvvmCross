using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Android.App;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid.Platform;
using Cirrious.MvvmCross.ViewModels;
using MvvmCross.Droid.Support.V7.Fragging.Attributes;

namespace MvvmCross.Droid.Support.V7.Fragging.Presenter
{
    internal class FragmentHostRegistrationSettings
    {
        private readonly Func<IEnumerable<Assembly>> _assembliesToLookupProvider;
        private readonly IMvxViewModelTypeFinder _viewModelTypeFinder;

        private Dictionary<Type, MvxFragmentAttribute> _fragmentTypeToMvxFragmentAttributeMap;
        private Dictionary<Type, Type> _viewModelToFragmentMap;

        private bool isInitialized;

        public FragmentHostRegistrationSettings(Func<IEnumerable<Assembly>> assembliesToLookupProvider)
        {
            _assembliesToLookupProvider = assembliesToLookupProvider;
            _viewModelTypeFinder = Mvx.Resolve<IMvxViewModelTypeFinder>();
        }

        private void InitializeIfNeeded()
        {
            lock (this)
            {
                if (isInitialized)
                    return;

                isInitialized = true;

                var assembliesToLookIn = _assembliesToLookupProvider();
                var typesWithMvxFragmentAttribute =
                    assembliesToLookIn
                        .SelectMany(x => x.DefinedTypes)
                        .Select(x => x.AsType())
                        .Where(x => x.HasMvxFragmentAttribute())
                        .ToList();

                _fragmentTypeToMvxFragmentAttributeMap = typesWithMvxFragmentAttribute.ToDictionary(key => key, key => key.GetMvxFragmentAttribute());
                _viewModelToFragmentMap =
                    typesWithMvxFragmentAttribute.ToDictionary(GetAssociatedViewModelType, fragmentType => fragmentType);
            }
        }

        private Type GetAssociatedViewModelType(Type fromFragmentType)
        {
            Type viewModelType = _viewModelTypeFinder.FindTypeOrNull(fromFragmentType);

            return viewModelType ?? fromFragmentType.GetMvxFragmentAttribute().ViewModelType;
        }

        public bool IsTypeRegisteredAsFragment(Type viewModelType)
        {
            InitializeIfNeeded();

            return _viewModelToFragmentMap.ContainsKey(viewModelType);
        }

        public bool IsActualHostValid(Type forViewModelType)
        {
            InitializeIfNeeded();

            Activity currentActivity = Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity;
            Type currentActivityType = currentActivity.GetType();

            var activityViewModelType = _viewModelTypeFinder.FindTypeOrNull(currentActivityType);

            if (activityViewModelType == null)
                throw new InvalidOperationException($"Sorry but looks like your Activity ({currentActivityType.ToString()}) does not inherit from MvvmCross Activity - Viewmodel Type is null!");

            return GetMvxFragmentAttributeAssociated(forViewModelType).ParentActivityViewModelType == activityViewModelType;

        }
        public Type GetFragmentHostViewModelType(Type forViewModelType)
        {
            InitializeIfNeeded();

            return GetMvxFragmentAttributeAssociated(forViewModelType).ParentActivityViewModelType;
        }

        public Type GetFragmentTypeAssociatedWith(Type viewModelType)
        {
            return _viewModelToFragmentMap[viewModelType];
        }

        public MvxFragmentAttribute GetMvxFragmentAttributeAssociated(Type withFragmentForViewModelType)
        {
            var fragmentType = GetFragmentTypeAssociatedWith(withFragmentForViewModelType);
            return _fragmentTypeToMvxFragmentAttributeMap[fragmentType];
        }
    }
}