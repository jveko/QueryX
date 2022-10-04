using Microsoft.AspNetCore.Mvc;
using QueryX.Samples.WebApi.Dtos;
using QueryX.Samples.WebApi.DataAccess;
using QueryX.Samples.WebApi.Domain.Model;
using Microsoft.EntityFrameworkCore;
using QueryX.Samples.WebApi.Queries;
using Mapster;

namespace QueryX.Samples.WebApi.Controllers
{
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly WorkboardContext _context;
        private readonly QueryBuilder _queryBuilder;

        public UsersController(WorkboardContext context, QueryBuilder queryBuilder)
        {
            _context = context;
            _queryBuilder = queryBuilder;
        }

        [HttpGet("{id}", Name = "GetUserById")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null)
                return NotFound();

            return Ok(new ResultModel<UserDto>
            {
                Data = user.Adapt<UserDto>()
            });
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] QueryModel queryModel, CancellationToken cancellationToken)
        {
            var query = _queryBuilder.CreateQuery<UserDto>(queryModel);

            var queryable = _context.Set<User>().AsNoTracking();

            var a = queryable.Where(u => u.Id == 1 || u.Id == 2 && u.Id == 3).ToList();
            var b = queryable.Where(u => u.Id == 3 && u.Id == 1 || u.Id == 2).ToList();
            var c = queryable.Where(u => (u.Id == 1 || u.Id == 2) && u.Id == 3).ToList();
            var d = queryable.Where(u => u.Id == 3 && (u.Id == 1 || u.Id == 2)).ToList();

            queryable = queryable.ApplyQuery(query, applyOrderingAndPaging: false);
            var totalCount = queryable.Count();
            queryable = queryable.ApplyOrderingAndPaging(query);

            var result = (await queryable.ToListAsync(cancellationToken))
                            .Adapt<List<UserDto>>();

            return Ok(new ResultModel<List<UserDto>>
            {
                Data = result,
                TotalCount = totalCount
            });
        }
    }
}
