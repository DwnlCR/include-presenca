using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Application.Exceptions;

namespace IncludePresenca.Application.UseCases.Identity;

public class CreateMemberUseCase
{
    private readonly IMembroRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMemberUseCase(
        IMembroRepository membroRepository,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = membroRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(CreateMemberRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        if (await _memberRepository.ExistsByEmailAsync(email))
        {
            throw new EmailAlreadyExistsException();
        }

        Membro membro = new Membro(
            request.Name,
            email,
            request.Role
        );

        await _memberRepository.AddAsync(membro);
        await _unitOfWork.SaveChangesAsync();

        return membro.Id;
    }
}