﻿using System;

using Norma.Eta.Properties;

namespace Norma.Eta.Validations
{
    public class StringRequiredValidator : IValidator<string>
    {
        #region Implementation of IValidator<string>

        public string Validate(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Resources.KeywordCannotBeBlank : null;
        }

        public string Convert(string value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}