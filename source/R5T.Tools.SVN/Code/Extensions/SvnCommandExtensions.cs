using System;
using System.Collections.Generic;
using System.Linq;

using R5T.NetStandard.Extensions;
using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public static class SvnCommandExtensions
    {
        #region Common

        public static void Add(this SvnCommand svnCommand, AbsolutePath path)
        {
            SvnCommandServicesProvider.Add(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
        }

        /// <summary>
        /// Commits changes to the specified path and returns the revision number.
        /// For directory paths, to commit only changes to the directory (for example, changes to SVN properties of the directory) and not changes within the directory, set the include all changes within path input to false.
        /// </summary>
        public static int Commit(this SvnCommand svnCommand, AbsolutePath path, string message, bool includeAllChangesWithinPath = true)
        {
            var revision = SvnCommandServicesProvider.Commit(svnCommand.SvnExecutableFilePath, path, message, svnCommand.Logger, includeAllChangesWithinPath);
            return revision;
        }

        public static bool HasProperty(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            var output = SvnCommandServicesProvider.HasProperty(svnCommand.SvnExecutableFilePath, path, propertyName, svnCommand.Logger);
            return output;
        }

        public static string[] ListProperties(this SvnCommand svnCommand, AbsolutePath path)
        {
            var output = SvnCommandServicesProvider.ListProperties(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
            return output;
        }

        public static void Delete(this SvnCommand svnCommand, AbsolutePath path, bool force = false)
        {
            SvnCommandServicesProvider.Delete(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger, force);
        }

        public static void DeleteProperty(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            SvnCommandServicesProvider.DeleteProperty(svnCommand.SvnExecutableFilePath, path, propertyName, svnCommand.Logger);
        }

        /// <summary>
        /// Gets the value of an SVN property.
        /// If the property does not exist, an exception is thrown.
        /// </summary>
        public static string GetPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            var output = SvnCommandServicesProvider.GetPropertyValue(svnCommand.SvnExecutableFilePath, path, propertyName, svnCommand.Logger);
            return output;
        }

        /// <summary>
        /// Gets the value of an SVN property.
        /// If the property does not exist, the <see cref="SvnCommand.NonValue"/> is returned.
        /// </summary>
        public static string GetPropertyValueIfExists(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            if (!svnCommand.HasSvnIgnoreProperty(path))
            {
                return SvnCommand.NonValue;
            }

            var output = svnCommand.GetPropertyValue(path, propertyName);
            return output;
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method gets the values in the property value set.
        /// </summary>
        public static string[] GetPropertyValues(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            var value = svnCommand.GetPropertyValueIfExists(path, propertyName);
            if (SvnCommand.IsValue(value))
            {
                var output = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return output;
            }
            else
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Many SVN properties have values that are lists of new-line separated strings.
        /// This method tests if a list property has a given value as an element.
        /// </summary>
        public static bool HasListPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            var propertyValues = svnCommand.GetPropertyValues(path, propertyName);

            var output = propertyValues.Contains(value);
            return output;
        }

        /// <summary>
        /// Sets the value for an SVN property.
        /// </summary>
        public static void SetPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            SvnCommandServicesProvider.SetPropertyValue(svnCommand.SvnExecutableFilePath, path, propertyName, value, svnCommand.Logger);
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method sets the values in the property value set.
        /// </summary>
        public static void SetPropertyValues(this SvnCommand svnCommand, AbsolutePath path, string propertyName, params string[] values)
        {
            var value = String.Join("\n", values);

            svnCommand.SetPropertyValue(path, propertyName, value);
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method adds a value to that set.
        /// This method will not add the same value twice, as it checks if the value is already present in the set of values.
        /// </summary>
        public static void AddPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            var values = new List<string>(svnCommand.GetPropertyValues(path, propertyName));

            // Only add the value once.
            if (!values.Contains(value))
            {
                values.Add(value);

                svnCommand.SetPropertyValues(path, propertyName, values.ToArray());
            }
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method removes a value from that set.
        /// Idempotent, can be called mulitple times.
        /// If, after removing the specified value, no values remain, the property will be deleted.
        /// </summary>
        public static void RemovePropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            var values = new List<string>(svnCommand.GetSvnIgnoreValues(path));

            values.Remove(value);

            if (values.IsEmpty())
            {
                svnCommand.DeleteProperty(path, propertyName);
            }
            else
            {
                svnCommand.SetSvnIgnoreValues(path, values.ToArray());
            }
        }

        public static Version GetVersion(this SvnCommand svnCommand)
        {
            var version = SvnCommandServicesProvider.GetVersion(svnCommand.SvnExecutableFilePath, svnCommand.Logger);
            return version;
        }

        public static void Revert(this SvnCommand svnCommand, AbsolutePath path)
        {
            SvnCommandServicesProvider.Revert(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
        }

        public static int Update(this SvnCommand svnCommand, AbsolutePath path)
        {
            var revision = SvnCommandServicesProvider.Update(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
            return revision;
        }

        #endregion

        #region SVN Ignore

        public static bool HasSvnIgnoreProperty(this SvnCommand svnCommand, AbsolutePath path)
        {
            var output = svnCommand.HasProperty(path, SvnCommand.SvnIgnorePropertyName);
            return output;
        }

        /// <summary>
        /// Sets the whole value of the SVN ignore property.
        /// </summary>
        public static void SetSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            SvnCommandServicesProvider.SetPropertyValue(svnCommand.SvnExecutableFilePath, path, SvnCommand.SvnIgnorePropertyName, value, svnCommand.Logger);
        }

        /// <summary>
        /// Gets the whole value of the SVN ignore property, or <see cref="SvnCommand.NonValue"/> if not.
        /// </summary>
        public static string GetSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path)
        {
            var value = svnCommand.GetPropertyValueIfExists(path, SvnCommand.SvnIgnorePropertyName);
            return value;
        }

        /// <summary>
        /// Deletes the SVN ignore property.
        /// Idempotent.
        /// </summary>
        public static void DeleteSvnIgnore(this SvnCommand svnCommand, AbsolutePath path)
        {
            svnCommand.DeleteProperty(path, SvnCommand.SvnIgnorePropertyName);
        }

        public static void SetSvnIgnoreValues(this SvnCommand svnCommand, AbsolutePath path, params string[] values)
        {
            svnCommand.SetPropertyValues(path, SvnCommand.SvnIgnorePropertyName, values);
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// Gets the set of values of the SVN ignore property.
        /// </summary>
        public static string[] GetSvnIgnoreValues(this SvnCommand svnCommand, AbsolutePath path)
        {
            var output = svnCommand.GetPropertyValues(path, SvnCommand.SvnIgnorePropertyName);
            return output;
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// This method tests if the set of SVN ignore values containes the specified value.
        /// </summary>
        public static bool HasSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            var output = svnCommand.HasListPropertyValue(path, SvnCommand.SvnIgnorePropertyName, value);
            return output;
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// This method adds a value to the set.
        /// Idempotent, will only add a valice once since you can only ignore a value once.
        /// </summary>
        public static void AddSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            svnCommand.AddPropertyValue(path, SvnCommand.SvnIgnorePropertyName, value);
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// This method allows removing one of those strings while keeping the rest.
        /// </summary>
        public static void RemoveSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            svnCommand.RemovePropertyValue(path, SvnCommand.SvnIgnorePropertyName, value);
        }

        #endregion
    }
}
