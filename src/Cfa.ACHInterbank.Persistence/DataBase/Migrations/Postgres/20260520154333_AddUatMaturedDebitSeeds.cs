using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddUatMaturedDebitSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    source_fi_id integer;
                    origin_dfi text;
                    company_entry_id integer;
                    ach_cycle_id text;
                    cenit_cycle_id text;
                    ach_batch_id integer;
                    cenit_batch_id integer;
                    ach_dest_id integer;
                    cenit_dest_id integer;
                    tx_id integer;
                    effective_date timestamptz := current_date;
                BEGIN
                    SELECT "Id", ("RoutingNumber" || "TransitCode")
                    INTO source_fi_id, origin_dfi
                    FROM "FinancialInstitutions"
                    WHERE "IsDefaultSource" = true
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT "Id" INTO company_entry_id
                    FROM "CompanyEntryDescription"
                    WHERE "Term" = 'PAGOS PSE'
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT "Id" INTO ach_dest_id
                    FROM "FinancialInstitutions"
                    WHERE "Name" = 'Banco UAT Destino'
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT "Id" INTO cenit_dest_id
                    FROM "FinancialInstitutions"
                    WHERE "Name" = 'Banco UAT Destino CENIT'
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT c."Id" INTO ach_cycle_id
                    FROM "AchCycles" c
                    JOIN "ClearingHouses" ch ON ch."Id" = c."ClearingHouseId"
                    WHERE ch."Code" IN ('ACH', 'ACHCOL')
                      AND c."ProcessingDate" = current_date
                      AND c."CycleName" = 'Ciclo 4'
                    LIMIT 1;

                    SELECT c."Id" INTO cenit_cycle_id
                    FROM "AchCycles" c
                    JOIN "ClearingHouses" ch ON ch."Id" = c."ClearingHouseId"
                    WHERE ch."Code" = 'CENIT'
                      AND c."ProcessingDate" = current_date
                      AND c."CycleName" = 'Ciclo 4'
                    LIMIT 1;

                    IF source_fi_id IS NOT NULL AND company_entry_id IS NOT NULL AND ach_cycle_id IS NOT NULL AND ach_dest_id IS NOT NULL THEN
                        SELECT "Id" INTO ach_batch_id FROM "AchBatches" WHERE "AchCycleId" = ach_cycle_id ORDER BY "Id" LIMIT 1;
                        IF ach_batch_id IS NULL THEN
                            INSERT INTO "AchBatches"
                                ("AchCycleId","ServiceClassCode","CompanyName","CompanyIdentification","CompanyEntryDescription",
                                 "CompanyEntryDescriptionId","OriginOrOdfi","EffectiveEntryDate","BatchSequenceNumber",
                                 "TotalDebitAmount","TotalCreditAmount","CreatedAt","UpdatedAt")
                            VALUES
                                (ach_cycle_id,'225','UAT SINT','900000001','PAGOS PSE',
                                 company_entry_id,origin_dfi,effective_date,1,1000,0,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO ach_batch_id;
                        END IF;

                        IF NOT EXISTS (SELECT 1 FROM "AchTransactions" WHERE "Reference" = 'UAT-ACH-DEB-MATURED-001') THEN
                            INSERT INTO "AchTransactions"
                                ("Amount","TransactionExternalId","Reference","Type","TransactionCode","ServiceClassCode",
                                 "CompanyEntryDescriptionId","CompanyName","CompanyIdentification","OriginatingDFI","ReceivingDFI",
                                 "TraceNumber","TraceSequenceNumber","EffectiveEntryDate","AddendaRecordIndicator","IsPrenotification",
                                 "State","StateChangedAtUtc","ContrapartidasResponseCode","ReturnReasonCode","OriginalTraceRef",
                                 "RecipientIdNumber","DiscretionaryData","SourceAccountNumber","DestinationAccountNumber",
                                 "SourceInstitutionId","DestinationInstitutionId","AchCycleId","AchBatchId","CustomerId","CreatedAt","UpdatedAt")
                            VALUES
                                (1000,'UAT-ACH-DEB-MATURED-001','UAT-ACH-DEB-MATURED-001','Debit','27','225',
                                 company_entry_id,'UAT SINT','900000001',origin_dfi,'99999002',
                                 origin_dfi || '090003',90003,effective_date,true,false,
                                 'Pending',timezone('utc', now()),'','','','900000101','UT','0000001101','0000001102',
                                 source_fi_id,ach_dest_id,ach_cycle_id,ach_batch_id,null,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO tx_id;

                            INSERT INTO "AchTransactionAddenda"
                                ("AchTransactionId","AddendaType","BusinessType","Information","Purpose","Reference","SequenceNumber","CreatedAt","UpdatedAt")
                            VALUES
                                (tx_id,'05','Debit','DEBITO UAT ACH','RECAUDO','UATACHDEB001',1,timezone('utc', now()),timezone('utc', now()));

                            INSERT INTO "AchTransactionStateEvents"
                                ("AchTransactionId","FromState","ToState","Source","ReasonCode","PayloadJson","CreatedAt","UpdatedAt")
                            VALUES
                                (tx_id,'Pending','Pending','System','UAT_DEBIT_CREATED',
                                 '{"scope":"UAT","control":"monetary-debit-after-mature-prenote","externalTransmission":false}',
                                 timezone('utc', now()),timezone('utc', now()));
                        END IF;
                    END IF;

                    IF source_fi_id IS NOT NULL AND company_entry_id IS NOT NULL AND cenit_cycle_id IS NOT NULL AND cenit_dest_id IS NOT NULL THEN
                        SELECT "Id" INTO cenit_batch_id FROM "AchBatches" WHERE "AchCycleId" = cenit_cycle_id ORDER BY "Id" LIMIT 1;
                        IF cenit_batch_id IS NULL THEN
                            INSERT INTO "AchBatches"
                                ("AchCycleId","ServiceClassCode","CompanyName","CompanyIdentification","CompanyEntryDescription",
                                 "CompanyEntryDescriptionId","OriginOrOdfi","EffectiveEntryDate","BatchSequenceNumber",
                                 "TotalDebitAmount","TotalCreditAmount","CreatedAt","UpdatedAt")
                            VALUES
                                (cenit_cycle_id,'225','UAT SINT','900000001','PAGOS PSE',
                                 company_entry_id,origin_dfi,effective_date,1,1000,0,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO cenit_batch_id;
                        END IF;

                        IF NOT EXISTS (SELECT 1 FROM "AchTransactions" WHERE "Reference" = 'UAT-CEN-DEB-MATURED-001') THEN
                            INSERT INTO "AchTransactions"
                                ("Amount","TransactionExternalId","Reference","Type","TransactionCode","ServiceClassCode",
                                 "CompanyEntryDescriptionId","CompanyName","CompanyIdentification","OriginatingDFI","ReceivingDFI",
                                 "TraceNumber","TraceSequenceNumber","EffectiveEntryDate","AddendaRecordIndicator","IsPrenotification",
                                 "State","StateChangedAtUtc","ContrapartidasResponseCode","ReturnReasonCode","OriginalTraceRef",
                                 "RecipientIdNumber","DiscretionaryData","SourceAccountNumber","DestinationAccountNumber",
                                 "SourceInstitutionId","DestinationInstitutionId","AchCycleId","AchBatchId","CustomerId","CreatedAt","UpdatedAt")
                            VALUES
                                (1000,'UAT-CEN-DEB-MATURED-001','UAT-CEN-DEB-MATURED-001','Debit','27','225',
                                 company_entry_id,'UAT SINT','900000001',origin_dfi,'99998002',
                                 origin_dfi || '090004',90004,effective_date,true,false,
                                 'Pending',timezone('utc', now()),'','','','900000102','UT','0000001201','0000001202',
                                 source_fi_id,cenit_dest_id,cenit_cycle_id,cenit_batch_id,null,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO tx_id;

                            INSERT INTO "AchTransactionAddenda"
                                ("AchTransactionId","AddendaType","BusinessType","Information","Purpose","Reference","SequenceNumber","CreatedAt","UpdatedAt")
                            VALUES
                                (tx_id,'05','Debit','DEBITO UAT CEN','RECAUDO','UATCENDEB001',1,timezone('utc', now()),timezone('utc', now()));

                            INSERT INTO "AchTransactionStateEvents"
                                ("AchTransactionId","FromState","ToState","Source","ReasonCode","PayloadJson","CreatedAt","UpdatedAt")
                            VALUES
                                (tx_id,'Pending','Pending','System','UAT_DEBIT_CREATED',
                                 '{"scope":"UAT","control":"monetary-debit-after-mature-prenote","externalTransmission":false}',
                                 timezone('utc', now()),timezone('utc', now()));
                        END IF;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "AchTransactionStateEvents"
                WHERE "AchTransactionId" IN (
                    SELECT "Id" FROM "AchTransactions"
                    WHERE "Reference" IN ('UAT-ACH-DEB-MATURED-001','UAT-CEN-DEB-MATURED-001')
                );

                DELETE FROM "AchTransactionAddenda"
                WHERE "AchTransactionId" IN (
                    SELECT "Id" FROM "AchTransactions"
                    WHERE "Reference" IN ('UAT-ACH-DEB-MATURED-001','UAT-CEN-DEB-MATURED-001')
                );

                DELETE FROM ONLY "AchTransactions"
                WHERE "Reference" IN ('UAT-ACH-DEB-MATURED-001','UAT-CEN-DEB-MATURED-001');
                """);
        }
    }
}
