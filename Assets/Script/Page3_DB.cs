using UnityEngine;
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using TMPro;
using UnityEngine.UI;

public class Page3_DB : MonoBehaviour
{
    // Connection details
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    [Header("Input Object")]
    public TMP_InputField title;
    public TMP_InputField author;
    public TMP_InputField publisher;
    public TMP_InputField price;
    public TMP_InputField url;
    public RawImage bookPhoto;
    public TMP_InputField isbn;
    public TMP_InputField description;

    [Header("Pop-Up Object")]
    public GameObject successUI;
    public GameObject failUI;
    public TMP_Text failText;

    public void DataBaseConnection()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("INSERT_BOOK", connection);
            command.CommandType = CommandType.StoredProcedure;

            try
            {
                string price = this.price.text.Replace(",", "").Replace("원", "");

                byte[] book_photo = null;
                if (this.bookPhoto.texture != null)
                {
                    Texture2D texture = this.bookPhoto.texture as Texture2D;
                    book_photo = texture.EncodeToPNG();
                }

                command.Parameters.Add("B_TITLE", OracleDbType.NVarchar2).Value = title.text;
                command.Parameters.Add("B_AUTHOR", OracleDbType.NVarchar2).Value = author.text;
                command.Parameters.Add("B_PUBLISHER", OracleDbType.NVarchar2).Value = publisher.text;
                command.Parameters.Add("B_PRICE", OracleDbType.Decimal).Value = (string.IsNullOrEmpty(price)) ? DBNull.Value : price;
                command.Parameters.Add("B_URL", OracleDbType.Clob).Value = (string.IsNullOrEmpty(url.text)) ? DBNull.Value : url.text;
                command.Parameters.Add("B_BOOKPHOTO", OracleDbType.Blob).Value = (book_photo == null) ? DBNull.Value : book_photo;
                command.Parameters.Add("B_ISBN", OracleDbType.Decimal).Value = isbn.text;
                command.Parameters.Add("B_DESCRIPTION", OracleDbType.Clob).Value = (string.IsNullOrEmpty(description.text)) ? DBNull.Value : description.text;

                command.ExecuteNonQuery();

                successUI.SetActive(true);
            }
            catch (Exception ex)
            {
                failText.text = "필수 입력사항이 누락되거나 \n중복된 도서 정보가 있습니다.";
                failUI.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            failText.text = "DB 연결에 실패했습니다.";
            failUI.SetActive(true);
        }
        finally
        {
            connection.Close();
        }
    }
}
