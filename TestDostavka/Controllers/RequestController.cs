using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Security.Claims;
using TestDostavka.Migrations;
using TestDostavka.Models.Enums;

namespace TestDostavka.Controllers
{
    [Route("Request")]
    public class RequestController : Controller
    {
        private readonly IYooKassaService _yooKassaService;
        private readonly AppDbContext _dbContext;

        public RequestController(
            IYooKassaService yooKassaService,
            AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _yooKassaService = yooKassaService;
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var personId = GetCurrentPersonId();

            var requests = await _dbContext.Requests.AsNoTracking().Where(x => x.PersonId == personId)
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreationDateTime)
                .Select(r => new CustomerRequestsListModel
                {
                    Id = r.Id,
                    ProductName = r.ProductName,
                    ProductUrl = r.ProductUrl,
                    Status = r.Status,
                    DateTime = r.CreationDateTime,
                    DeliveryDate = r.DeliveryDate
                }).ToListAsync(cancellationToken);

            return View(requests);
        }

        [Authorize(Roles = nameof(PersonRole.Manager) + "," + nameof(PersonRole.Administrator))]
        [HttpGet("Manage")]
        public async Task<IActionResult> Manage([FromQuery] ManagerRequestsFilterModel filter, CancellationToken cancellationToken)
        {
            var query = _dbContext.Requests.AsNoTracking();

            if (filter.Statuses.Count > 0)
                query = query.Where(r => filter.Statuses.Contains(r.Status));

            if (!string.IsNullOrWhiteSpace(filter.Email))
                query = query.Where(r => r.Person.Email.ToLower().Contains(filter.Email.Trim().ToLower()));

            if (filter.DateFrom.HasValue)
            {
                var dateFromUtc = DateTime.SpecifyKind(
                    filter.DateFrom.Value.Date,
                    DateTimeKind.Utc);

                query = query.Where(r =>
                    r.CreationDateTime >= dateFromUtc);
            }

            if (filter.DateTo.HasValue)
            {
                var dateToExclusiveUtc = DateTime.SpecifyKind(
                    filter.DateTo.Value.Date.AddDays(1),
                    DateTimeKind.Utc);

                query = query.Where(r =>
                    r.CreationDateTime < dateToExclusiveUtc);
            }

            var requests = await query
                .OrderBy(r => r.Status)
                .ThenBy(r => r.CreationDateTime)
                .Select(r => new ManagerRequestsListModel
                {
                    Id = r.Id,
                    CustomerEmail = r.Person.Email,
                    ProductName = r.ProductName,
                    ProductUrl = r.ProductUrl,
                    Status = r.Status,
                    DateTime = r.CreationDateTime,
                    DeliveryDate = r.DeliveryDate
                }).ToListAsync(cancellationToken);

            var pageModel = new ManagerRequestsPageModel
            {
                Filter = filter,
                Requests = requests
            };

            return View(pageModel);
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpGet("RequestCreate")]
        public IActionResult RequestCreate()
        {
            return View();
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpPost("RequestCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCreate(CreateRequestModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var personId = GetCurrentPersonId();

            var requestId = Guid.NewGuid();

            var request = new Request
            {
                Id = requestId,
                PersonId = personId,
                ProductName = model.ProductName.Trim(),
                ProductUrl = model.ProductUrl.Trim(),
                Description = model.Description?.Trim(),
                Status = RequestStatus.UnderReview,
                CreationDateTime = DateTime.UtcNow,
                Quantity = model.Quantity,
            };

            _dbContext.Requests.Add(request);

            await _dbContext.RequestComments.AddAsync(new RequestComment
            {
                IsFromCustomer = true,
                RequestId = requestId,
                Comment = "",
                TechComment = $"Создал заявку."
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(
                nameof(RequestCard),
                new { requestId = request.Id });
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> RequestCard([FromRoute] Guid requestId, CancellationToken cancellationToken)
        {
            var isCustomer = User.IsInRole(nameof(PersonRole.Customer));

            var request = await _dbContext.Requests
                .AsNoTracking()
                .Where(r => r.Id == requestId)
                .Select(r => new RequestCardModel
                {
                    Id = r.Id,
                    PersonId = r.PersonId,
                    ProductName = r.ProductName,
                    ProductUrl = r.ProductUrl,
                    Description = r.Description,
                    CustomerEmail = r.Person.Email,
                    Status = r.Status,
                    OfferedPrice = r.Price,
                    CreationDateTime = r.CreationDateTime,
                    DeliveryDate = r.DeliveryDate,
                    IsCustomer = isCustomer,
                    IsManager = !isCustomer,
                    Comments = _dbContext.RequestComments.AsNoTracking().Where(c => c.RequestId == requestId).OrderByDescending(comment => comment.CreationDateTime).ToList()
                }).FirstOrDefaultAsync(cancellationToken);
            var personId = GetCurrentPersonId();
            var person = await _dbContext.Persons.FirstOrDefaultAsync(p => p.Id == personId);

            if (person == null) 
                return NotFound("Пользователь не зарегистрирован в базе.");

            if (request == null)
                return NotFound("Такого Запроса не существует");

            if (person.Role == PersonRole.Customer && request.PersonId != personId)
                return BadRequest("У вас нет доступа к чужим запросам.");


            return View(request);
        }

        [HttpGet("RedirectUrl")]
        public IActionResult RedirectUrl()
        {
            var personRole = _dbContext.Persons.FirstOrDefault(p => p.Id == GetCurrentPersonId())?.Role;

            if (personRole == null || personRole == PersonRole.Customer)
            {
                return RedirectToAction("Index", "Request");
            }

            return RedirectToAction("Manage", "Request");
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpPost("{requestId}/Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest([FromRoute] Guid requestId, CancellationToken cancellationToken)
        {
            var personId = GetCurrentPersonId();

            var request = await _dbContext.Requests.FirstOrDefaultAsync( r => r.Id == requestId && r.PersonId == personId, cancellationToken);

            if (request is null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                RequestStatus.UnderReview,
                RequestStatus.OfferReady,
                RequestStatus.OfferAccepted
            };

            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest("Заявку в текущем статусе нельзя отменить.");
            }

            await _dbContext.RequestComments.AddAsync(new RequestComment
            {
                IsFromCustomer = true,
                RequestId = requestId,
                Comment = "",
                TechComment = $"Отменил заявку."
            });

            request.Status = RequestStatus.Cancelled;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId });
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpPost("{requestId}/ProductPickedUp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductPickedUp([FromRoute] Guid requestId, CancellationToken cancellationToken)
        {
            var personId = GetCurrentPersonId();

            var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == requestId && r.PersonId == personId, cancellationToken);

            if (request is null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.ReadyForPickUp)
            {
                return BadRequest("Подтвердить получение заказа в текущем статусе нельзя.");
            }

            request.Status = RequestStatus.Completed;
            await _dbContext.RequestComments.AddAsync(new RequestComment
            {
                IsFromCustomer = true,
                RequestId = requestId,
                Comment = "",
                TechComment = $"Забрал заказ."
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId });
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpPost("{requestId}/AcceptOffer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer([FromRoute] Guid requestId, CancellationToken cancellationToken)
        {
            var personId = GetCurrentPersonId();

            var request = await _dbContext.Requests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.PersonId == personId, cancellationToken);

            if (request is null)
                return NotFound();

            if (request.Status != RequestStatus.OfferReady)
                return BadRequest("Принять можно только готовое предложение.");

            if (!request.Price.HasValue)
                return BadRequest("У заявки отсутствует предложенная цена.");

            request.Status = RequestStatus.OfferAccepted;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId });
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpPost("RejectOffer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOffer(RejectOfferModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(RequestCard), new { requestId = model.RequestId });
            }

            var personId = GetCurrentPersonId();

            var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == model.RequestId && r.PersonId == personId, cancellationToken);

            if (request is null)
                return NotFound();

            if (request.Status != RequestStatus.OfferReady)
                return BadRequest("Отклонить можно только готовое предложение.");

            await _dbContext.RequestComments.AddAsync(new RequestComment
            {
                IsFromCustomer = true,
                RequestId = model.RequestId,
                Comment = model.Comment == null ? "" : model.Comment.Trim(),
                TechComment = "Отверг предложение."
            });

            request.Status = RequestStatus.Rejected;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId = model.RequestId });
        }


        [Authorize(Roles = nameof(PersonRole.Manager) + "," + nameof(PersonRole.Administrator))]
        [HttpPost("CreateOffer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOffer(CreateOfferModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(RequestCard), new { requestId = model.RequestId });

            var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == model.RequestId, cancellationToken);

            if (request is null)
                return NotFound();

            if (request.Status != RequestStatus.UnderReview && request.Status != RequestStatus.Rejected)
                return BadRequest("В текущем статусе нельзя создать новое предложение.");

            request.Price = model.Price;
            
            await _dbContext.RequestComments.AddAsync(new RequestComment
            {
                IsFromCustomer = false,
                RequestId = model.RequestId,
                Comment = model.Comment == null ? "" : model.Comment.Trim(),
                TechComment = $"Создал предложение на сумму {model.Price}"
            });

            request.Status = RequestStatus.OfferReady;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId = model.RequestId });
        }


        [Authorize(Roles = nameof(PersonRole.Manager) + "," + nameof(PersonRole.Administrator))]
        [HttpPost("ChangeStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(ChangeRequestStatusModel model, CancellationToken cancellationToken)
        {
            var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == model.RequestId, cancellationToken);

            if (request is null)
                return NotFound();

            if (!Enum.IsDefined(model.NewStatus))
                return BadRequest("Некорректный статус.");

            request.Status = model.NewStatus;

            await _dbContext.RequestComments.AddAsync(new RequestComment
            {
                IsFromCustomer = false,
                RequestId = model.RequestId,
                Comment = model.Comment == null ? "" : model.Comment.Trim(),
                TechComment = $"Изменил статус на {model.NewStatus}"
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId = model.RequestId });
        }

        [Authorize(Roles = nameof(PersonRole.Manager) + "," + nameof(PersonRole.Administrator))]
        [HttpPost("SetDeliveryDate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDeliveryDate(SetDeliveryDateModel model, CancellationToken cancellationToken)
        {
            var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == model.RequestId, cancellationToken);

            if (request is null)
                return NotFound();

            var allowedStatuses = new[]
            {
                RequestStatus.UnderReview,
                RequestStatus.OfferReady,
                RequestStatus.OfferAccepted,
                RequestStatus.Paid,
                RequestStatus.Purchasing,
                RequestStatus.InDelivery,
            };

            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest("Изменить дату доставки в текущем статусе нельзя.");
            }

            request.DeliveryDate = model.DeliveryDate;

            await _dbContext.RequestComments.AddAsync(new RequestComment
            {
                IsFromCustomer = false,
                RequestId = model.RequestId,
                Comment = model.Comment == null ? "" : model.Comment.Trim(),
                TechComment = $"Изменил дату доставки на {model.DeliveryDate}"
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId = model.RequestId });
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpPost("{requestId}/PaymentTest")]
        public async Task<IActionResult> PaymentTest([FromRoute] Guid requestId, CancellationToken cancellationToken)
        {
            var personId = GetCurrentPersonId();

            var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == requestId && r.PersonId == personId, cancellationToken);

            if (request is null)
                return NotFound();

            if (request.Status != RequestStatus.OfferAccepted)
                return BadRequest("Заявка ещё не готова к оплате.");

            var paymentResult = new { status = true };

            if (paymentResult.status)
            {
                Console.WriteLine("Оплата");
                request.Status = RequestStatus.Paid;

                await _dbContext.RequestComments.AddAsync(new RequestComment
                {
                    IsFromCustomer = true,
                    RequestId = requestId,
                    Comment = "",
                    TechComment = $"Оплатил стоимость."
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(RequestCard), new { requestId });
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpPost("{requestId}/Payment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPayment(
            [FromRoute] Guid requestId,
            CancellationToken cancellationToken)
        {
            var personId = GetCurrentPersonId();

            var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == requestId && r.PersonId == personId, cancellationToken);

            if (request is null)
                return NotFound("Заявка не найдена.");

            if (request.Status != RequestStatus.OfferAccepted)
                return BadRequest("Оплатить можно только принятую заявку.");

            if (!request.Price.HasValue || request.Price.Value <= 0)
                return BadRequest("Для заявки не установлена корректная цена.");

            var activePayment = await _dbContext.Payments
                .Where(p =>
                    p.RequestId == request.Id && (
                        p.Status == PaymentStatus.Created ||
                        p.Status == PaymentStatus.Pending ||
                        p.Status == PaymentStatus.WaitingForCapture))
                .OrderByDescending(payment =>
                    payment.CreationDateTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (activePayment is not null)
                return await ContinueExistingPaymentAsync(activePayment, request, cancellationToken);

            var payment = new Payment
            {
                RequestId = request.Id,
                Amount = request.Price.Value,
                Currency = "RUB",
                Status = PaymentStatus.Created,
                IdempotencyKey = Guid.NewGuid().ToString(),
                CreationDateTime = DateTime.UtcNow,
                ModificationDateTime = DateTime.UtcNow
            };

            _dbContext.Payments.Add(payment);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await CreateProviderPaymentAsync(payment, request, cancellationToken);
        }

        [Authorize(Roles = nameof(PersonRole.Customer))]
        [HttpGet("PaymentResult/{requestId}")]
        public async Task<IActionResult> PaymentResult([FromRoute] Guid requestId, CancellationToken cancellationToken)
        {
            var personId = GetCurrentPersonId();

            var payment = await _dbContext.Payments
                .AsNoTracking()
                .Where(payment =>
                    payment.RequestId == requestId &&
                    payment.Request.PersonId == personId)
                .OrderByDescending(payment =>
                    payment.CreationDateTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (payment is null)
                return NotFound("Платёж не найден.");

            return View(payment);
        }


        private Guid GetCurrentPersonId()
        {
            var personIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(personIdStr, out Guid personId))
            {
                throw new InvalidOperationException("Пользователь отсутвтует в Cookies");
            }
            return personId;
        }

        private static PaymentStatus MapPaymentStatus(string providerStatus)
        {
            return providerStatus switch
            {
                "pending" => PaymentStatus.Pending,

                "waiting_for_capture" => PaymentStatus.WaitingForCapture,

                "succeeded" => PaymentStatus.Succeeded,

                "canceled" => PaymentStatus.Canceled,

                _ => PaymentStatus.Created
            };
        }

        private async Task<IActionResult> CreateProviderPaymentAsync(Payment payment, Request request, CancellationToken cancellationToken)
        {
            var returnUrl = Url.Action(
                action: nameof(PaymentResult),
                controller: "Request",
                values: new
                {
                    requestId = request.Id
                },
                protocol: HttpContext.Request.Scheme);

            if (string.IsNullOrWhiteSpace(returnUrl))
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Не удалось сформировать адрес возврата после оплаты.");

            try
            {
                var result = await _yooKassaService.CreatePaymentAsync(
                    new CreateYooKassaPaymentCommand
                    {
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        Description = $"Оплата заявки {request.Id}",
                        ReturnUrl = returnUrl,
                        RequestId = request.Id,
                        IdempotencyKey = payment.IdempotencyKey
                    },
                    cancellationToken);

                payment.ProviderPaymentId = result.Id;
                payment.ProviderStatus = result.Status;
                payment.Status = MapPaymentStatus(result.Status);
                payment.ConfirmationUrl = result.Confirmation?.ConfirmationUrl;
                payment.ModificationDateTime = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(payment.ConfirmationUrl))
                    return StatusCode(StatusCodes.Status502BadGateway, "Платёж создан, но платёжная система не вернула ссылку для оплаты.");

                return Redirect(payment.ConfirmationUrl);
            }
            catch (YooKassaApiException exception)
            {
                payment.ErrorCode = exception.ErrorCode;
                payment.ErrorDescription = exception.Message;
                payment.ModificationDateTime = DateTime.UtcNow;

                if ((int)exception.StatusCode >= 400 && (int)exception.StatusCode < 500)
                    payment.Status = PaymentStatus.Failed;

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["PaymentError"] = "Не удалось создать платёж. Попробуйте ещё раз.";

                return RedirectToAction(nameof(RequestCard), new { requestId = request.Id });
            }
            catch
            {
                payment.ErrorDescription = "Ошибка соединения с платёжной системой.";
                payment.ModificationDateTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["PaymentError"] = "Платёжная система временно недоступна.";
                return RedirectToAction(nameof(RequestCard), new { requestId = request.Id });
            }
        }

        private async Task<IActionResult> ContinueExistingPaymentAsync(Payment payment, Request request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
                return await CreateProviderPaymentAsync(payment, request, cancellationToken);

            try
            {
                var providerPayment = await _yooKassaService.GetPaymentAsync(payment.ProviderPaymentId, cancellationToken);

                payment.ProviderStatus = providerPayment.Status;
                payment.Status = MapPaymentStatus(providerPayment.Status);
                payment.ConfirmationUrl = providerPayment.Confirmation ?.ConfirmationUrl ?? payment.ConfirmationUrl;
                payment.ModificationDateTime = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

                if (payment.Status == PaymentStatus.Succeeded)
                    return RedirectToAction(nameof(PaymentResult), new { requestId = request.Id });

                if (payment.Status == PaymentStatus.Canceled)
                    return RedirectToAction(nameof(RequestCard), new { requestId = request.Id });

                if (!string.IsNullOrWhiteSpace(payment.ConfirmationUrl))
                    return Redirect(payment.ConfirmationUrl);

                return StatusCode(StatusCodes.Status502BadGateway, "У активного платежа отсутствует ссылка на оплату.");
            }
            catch
            {
                TempData["PaymentError"] = "Не удалось проверить текущий платёж.";
                return RedirectToAction(nameof(RequestCard), new { requestId = request.Id });
            }
        }
    }
}
