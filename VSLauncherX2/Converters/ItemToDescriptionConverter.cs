using System;
using System.Globalization;
using System.Windows.Data;
using VSLauncher.DataModel;

namespace VSLauncher.Converters
{
    public class ItemToDescriptionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return string.Empty;
            string desc = string.Empty;
            switch (value)
            {
                case VsSolution s:
                    if (s.Warning)
                    {
                        desc = "Could not find " + s.Path;
                    }
                    else
                    {
                        int projectCount = s.Projects?.Count ?? 0;
                        string plural = projectCount == 1 ? string.Empty : "s";
                        desc = $"Visual Studio Solution: {projectCount} Project{plural}, {s.TypeAsName()}";
                        if (VSLauncher.Properties.Settings.Default.ShowPathForSolutions)
                        {
                            desc += "\r\n" + s.Path;
                        }
                    }
                    break;
                case VsProject p:
                    if (p.Warning)
                    {
                        desc = "Could not find " + p.Path;
                    }
                    else
                    {
                        desc = $"Visual Studio Project: {p.TypeAsName()}, .NET {p.FrameworkVersion}";
                        if (VSLauncher.Properties.Settings.Default.ShowPathForSolutions)
                        {
                            desc += "\r\n" + p.Path;
                        }
                    }
                    break;
                case VsFolder f:
                    if (f.Warning)
                    {
                        desc = "Could not find " + f.Path;
                        break;
                    }
                    switch (f.ItemType)
                    {
                        case ItemTypeEnum.VisualStudio:
                            desc = f.Name;
                            break;
                        case ItemTypeEnum.Solution:
                        case ItemTypeEnum.Project:
                            desc = f.Path;
                            break;
                        case ItemTypeEnum.Folder:
                            int so = f.ContainedSolutionsCount();
                            int pr = f.ContainedProjectsCount();
                            if (so == 0 && pr == 0)
                            {
                                desc = "Contains nothing yet";
                            }
                            else
                            {
                                string projPlural = pr == 1 ? string.Empty : "s";
                                string solPlural = so == 1 ? string.Empty : "s";
                                if (pr > 0 && so > 0)
                                {
                                    desc = $"Contains {pr} project{projPlural} and {so} solution{solPlural}";
                                }
                                else if (pr > 0)
                                {
                                    desc = $"Contains {pr} project{projPlural}";
                                }
                                else
                                {
                                    desc = $"Contains {so} solution{solPlural}";
                                }
                            }
                            break;
                        default:
                            desc = string.Empty;
                            break;
                    }
                    break;
                case VsItem item:
                    desc = item.Path ?? string.Empty;
                    break;
            }
            return desc;
        }
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}