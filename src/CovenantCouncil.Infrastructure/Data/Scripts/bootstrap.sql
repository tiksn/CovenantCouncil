PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS protection_metadata (
  Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
  KdfAlgorithm TEXT NOT NULL,
  ProtectedKdfIterations BLOB NOT NULL,
  ProtectedKdfSalt BLOB NOT NULL,
  ProtectedKeyLength BLOB NOT NULL,
  ProtectedKeyRingXml BLOB NOT NULL,
  CreatedUtc TEXT NOT NULL,
  UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS parties (
  Id TEXT NOT NULL PRIMARY KEY,
  Kind TEXT NOT NULL,
  Email TEXT NULL,
  Website TEXT NULL,
  FirstName TEXT NULL,
  LastName TEXT NULL,
  FullName TEXT NULL,
  ShortName TEXT NULL,
  LongName TEXT NULL,
  CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS certificates (
  Id TEXT NOT NULL PRIMARY KEY,
  Thumbprint TEXT NOT NULL UNIQUE,
  Subject TEXT NOT NULL,
  Issuer TEXT NOT NULL,
  SerialNumber TEXT NOT NULL,
  NotBefore TEXT NOT NULL,
  NotAfter TEXT NOT NULL,
  ParentThumbprint TEXT NULL,
  RawDer BLOB NOT NULL,
  ImportedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS licenses (
  Id TEXT NOT NULL PRIMARY KEY,
  DescriptorDiscriminator TEXT NOT NULL,
  DescriptorName TEXT NOT NULL,
  PartyId TEXT NOT NULL,
  SigningCertificateThumbprint TEXT NOT NULL,
  ProtectedPayload BLOB NOT NULL,
  IssuedUtc TEXT NOT NULL,
  CONSTRAINT FK_licenses_parties_PartyId FOREIGN KEY (PartyId) REFERENCES parties (Id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_licenses_DescriptorDiscriminator ON licenses (DescriptorDiscriminator);
CREATE INDEX IF NOT EXISTS IX_licenses_PartyId ON licenses (PartyId);
CREATE INDEX IF NOT EXISTS IX_licenses_SigningCertificateThumbprint ON licenses (SigningCertificateThumbprint);
