namespace BookShop.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using BookShop.Data.Models;
    using BookShop.Data.Models.Enums;
    using BookShop.DataProcessor.ImportDto;
    using Data;
    using Newtonsoft.Json;
    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedBook
            = "Successfully imported book {0} for {1:F2}.";

        private const string SuccessfullyImportedAuthor
            = "Successfully imported author - {0} with {1} books.";

        public static string ImportBooks(BookShopContext context, string xmlString)
        {
            var output = new StringBuilder();
            // first make a Serializer
            var xmlSerializer = new XmlSerializer(typeof(XmlBookImportDto[]),
                                new XmlRootAttribute("Books")); //<--Root element="Books" in this exam
// we call Deserialize              ;
// String Reader = stream of strings = converts the string "xmlString" or it wont work
// And finally convert to the type we want = (XmlBookImportDto[])
            var books = (XmlBookImportDto[])xmlSerializer.Deserialize
                               (new StringReader(xmlString));

            var validBooks = new List<Book>();

            foreach (var xmlBook in books)
            {
                if(!IsValid(xmlBook))
                {
                    output.AppendLine(ErrorMessage);
                    continue;
                }

                bool parsedDate = DateTime.TryParseExact(
                    xmlBook.PublishedOn, "MM/dd/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

                if (!parsedDate)
                {
                    output.AppendLine("Invalid Data");
                    continue;
                }

                object genreObj;
                bool isGenreValid = Enum.TryParse(typeof(Genre), xmlBook.Genre, out genreObj);

                if (!isGenreValid)
                {
                    output.AppendLine(ErrorMessage);
                    continue;
                }


                var ourBook = new Book()
                {
                    Name = xmlBook.Name,
                    Genre = (Genre)genreObj,
                    Price = xmlBook.Price,
                    Pages = xmlBook.Pages,
                    PublishedOn = date
                };

                validBooks.Add(ourBook);
                output.AppendLine($"Successfully imported book {ourBook.Name} for {ourBook.Price}.");
            }
            context.Books.AddRange(validBooks);
            context.SaveChanges();

            return output.ToString().TrimEnd();
        }

        public static string ImportAuthors(BookShopContext context, string jsonString)
        {
            var output = new StringBuilder();
            var authors = JsonConvert.DeserializeObject<JsonAuthorImportDto[]>(jsonString);

            var validAuthors = new List<Author>();

            foreach (var jsonAuthor in authors)
            {
                if(!IsValid(jsonAuthor))
                {
                    output.AppendLine(ErrorMessage);
                    continue;
                }

                bool doesEmailExists = authors
                    .FirstOrDefault(x => x.Email == jsonAuthor.Email) != null;

                if (!doesEmailExists)
                {
                    output.AppendLine(ErrorMessage);
                    continue;
                }

                var ourAuthor = new Author()
                {
                    FirstName = jsonAuthor.FirstName,
                    LastName = jsonAuthor.LastName,
                    Email = jsonAuthor.Email,
                    Phone = jsonAuthor.Phone,
                };

                foreach (var jsonBook in jsonAuthor.Books)
                {
                    var ourBook = context.Books.Find(jsonBook.Id);

                    if(ourBook == null)
                    {
                        continue;
                    }
                       

                    ourAuthor.AuthorsBooks.Add(new AuthorBook
                    {
                        Author = ourAuthor,
                        Book = ourBook
                    });
                }

                if(ourAuthor.AuthorsBooks.Count() == 0)
                {
                    output.AppendLine(ErrorMessage);
                    continue;
                }


                validAuthors.Add(ourAuthor);
                output.AppendLine($"Successfully imported author" +
                    $" - {ourAuthor.FirstName + " " + ourAuthor.LastName} " +
                    $"with {ourAuthor.AuthorsBooks.Count()} books.");
            }
            context.Authors.AddRange(validAuthors);
            context.SaveChanges();

            return output.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}