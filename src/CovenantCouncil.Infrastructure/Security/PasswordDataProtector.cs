using Microsoft.AspNetCore.DataProtection;

namespace CovenantCouncil.Infrastructure.Security;

public sealed class PasswordDataProtector(IPortableProtectionService protectionService) : IDataProtector
{
  public IDataProtector CreateProtector(string purpose)
  {
    return this;
  }

  public byte[] Protect(byte[] plaintext)
  {
    return protectionService.Protect(plaintext);
  }

  public byte[] Unprotect(byte[] protectedData)
  {
    return protectionService.Unprotect(protectedData);
  }
}
