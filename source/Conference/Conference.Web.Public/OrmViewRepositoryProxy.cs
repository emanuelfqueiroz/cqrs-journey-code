// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Conference.Web.Public
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Common;
    using Registration.ReadModel;

    public class OrmViewRepositoryProxy : IViewRepository
    {
        public T Find<T>(Guid id) where T : class
        {
            using (var repo = new OrmViewRepository())
                return repo.Find<T>(id);
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            using (var repo = new OrmViewRepository())
                return repo.Query<T>(predicate);
        }
    }
}